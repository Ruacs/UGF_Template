# UI 模块使用手册

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. 职责与加载链

| 层 | 职责 | 不负责的内容 |
| --- | --- | --- |
| `GameEntry.UI` / `UIComponent` | 加载、实例化、分组、深度、打开关闭与实例池 | 业务数据和玩法暂停决策 |
| `UIExtension` | 将 `UIFormId` 转成表行，再按资源名打开页面 | 自动注册未登记页面、等待页面加载完成 |
| `UGuiForm` | Canvas、入场/关闭动画、多语言、事件钩子、页面 Item 池 | 统一取消业务异步任务、自动解除任意外部订阅 |
| 页面脚本 | 接收参数、刷新显示、转发用户操作、清理本页资源 | 替代存档/活动/游戏管理器持有全局业务状态 |
| Widget / `UIItemBase` | 页面内部的奖励格子、列表行、头像等复用视图 | 独立 UIForm ID 和窗口路由 |

```text
OpenUIForm(UIFormId, userData)
  → 已加载的 DRUIForm 表行
  → AssetUtility.GetUIFormAsset(AssetName)
  → 活动页面注册表 / 子游戏资源注册表 / 公共页面路径
  → UIComponent 加载资源或复用实例
  → UI Group → UGuiForm.OnInit（实例首次初始化）→ OnOpen（每次打开）
```

首次启动用的 `BuiltinView` 独立于这条表驱动开页链，见 [基础组件 §1](../Bootstrap/README.md#1-启动入口与内置加载视图)。

## 2. 调用方式与生命周期

### 2.1 打开、查询和关闭

以下是已有设置页的真实参数协议，可在主菜单资源就绪后使用：

```csharp
int? serialId = GameEntry.UI.OpenUIForm(
    UIFormId.SettingUIPanel,
    new SettingUIData(SettingType.Main));

// 按页面配置 ID 查询当前已打开实例（尚在加载时可能为 null）。
UGuiForm panel = GameEntry.UI.GetUIForm(UIFormId.SettingUIPanel);
panel?.Close();       // 先播关闭动画，再交给 GF 关闭。
// panel?.Close(false); // 立即关闭；按实际场景二选一。
```

设置页会强转 `SettingUIData`，不能作为“无参数开页”的例子。其他页先查自己的 `OnOpen` 参数契约。

**配置 ID 与实例 SerialId 必须分开：**

- `OpenUIForm(UIFormId, data)` 返回 `int?`，是请求/实例序列号。非空表示已发起，不代表 Prefab 加载成功。
- 返回 `null` 可能是组件/表未就绪、行缺失，也可能是单实例页已存在或正在加载。重复开页不会自动刷新参数或置顶。
- `GetUIForm(UIFormId)`、`HasUIForm(UIFormId)` 是项目扩展，按配置 ID 查询。
- **`GetUIForm(int)`、`HasUIForm(int)` 是 GF 原生实例方法，按 SerialId 查询。** 把枚举转为 int 再查询会改变含义；确需用整数配置 ID 时显式调用 `UIExtension.GetUIForm(GameEntry.UI, configId)`。
- `CloseUIForm(int)` 同样接收 SerialId，不能传 `(int)UIFormId.Xxx`。按配置找页后调用 `Close()`，或关闭此前保存的 SerialId。
- 需要确认完成时订阅 `OpenUIFormSuccessEventArgs` / `OpenUIFormFailureEventArgs`，按 SerialId/请求上下文过滤，成功后再访问实例。拥有者退出时解绑；加载中的请求也要用保存的 SerialId 关闭。
- 多实例页必须保存各自 SerialId；按配置查询只能取到匹配实例，无法表达“我要关哪一个”。

拥有者退出时按保存的序列号处理已开页或加载中请求，并先检查状态，避免重复关闭一个已不存在的实例：

```csharp
if (serialId.HasValue &&
    (GameEntry.UI.IsLoadingUIForm(serialId.Value) ||
     GameEntry.UI.HasUIForm(serialId.Value)))
{
    GameEntry.UI.CloseUIForm(serialId.Value);
}
```

这里的 `HasUIForm(int)` 有意使用 GF 原生的 SerialId 查询。框架对加载中请求记录“加载完成后释放”，不代表底层资源下载会立即中断。

### 2.2 页面脚本模板

以下代码是新增页面时的最小示例，类名和序列化字段应对应自己的 Prefab；不是项目中已经注册的页面。

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lokas
{
    public sealed class ExampleUIPanel : UGuiForm
    {
        [SerializeField] private Button m_CloseButton;
        [SerializeField] private TMP_Text m_Title;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_CloseButton.onClick.AddListener(OnClickClose);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            m_Title.text = userData as string ?? string.Empty;
        }

        private void OnClickClose() => Close();
    }
}
```

| 生命周期 | 推荐放什么 | 必须注意 |
| --- | --- | --- |
| OnInit | 缓存固定引用、一次性绑定本页按钮 | 先调用 base；复用实例时不会每次重跑 |
| OnOpen | 读取本次参数、刷新文本/列表/按钮状态 | 先调用 base；不要用 Start 代替每次开页刷新 |
| SubscribeEvents / UnsubscribeEvents | 对称订阅外部事件、商店/红点/活动通知 | 基类分别从 OnOpen/OnClose 调用；同一通知不要再重复绑定 |
| OnPause / OnResume | 页面被暂停与恢复时的业务响应 | 不等于调用游戏 PauseGame；派生行为按需实现 |
| OnCover / OnReveal | 同组遮盖/重新露出时的响应 | 跨组弹窗不要假定触发同样规则 |
| OnClose | 取消本次请求、停止本页业务 Tween/协程、清空临时引用 | 保留 base 调用；不能只等 OnDestroy |
| OnRecycle | 框架回收阶段的补充清理 | 关闭通常进入实例池，不保证立即销毁 |

`UGuiForm` 只处理其已实现的事件钩子与 Item 池，不会自动取消任意业务 UniTask。关闭时取消的 token 应由页面每次 OnOpen 创建、OnClose 取消；仅使用销毁 token 无法覆盖实例池中的“已关闭但未销毁”。异步完成后还要确认本次打开仍有效。

`AddSafeClick` 用闭包做点击节流，应放在 OnInit 等一次性绑定处；不要每次 OnOpen 累加。`AddOnceClick` 会禁用按钮，复用开页时需要明确恢复 interactable。

## 3. 排错顺序

| 现象 | 优先检查 |
| --- | --- |
| OpenUIForm 返回 null | 表是否就绪、ID 是否存在、单实例是否已开/加载中 |
| 返回 SerialId 但看不到页面 | 开页失败事件/Console、资源路径/收集、UI Group、根脚本与激活状态 |
| 子游戏页被加载到公共路径 | 子游戏清单是否登记 AssetName、全局是否引用、预加载是否成功 |
| 活动页找不到或参数不对 | Catalog 安装、Definition 页面清单、活动作用域与 OnOpen 参数 |
| GetUIForm 找不到已开的页 | 是否误用 int 配置 ID 调到了原生 SerialId 重载；是否还在加载 |
| 关闭了别的页 | 是否将 UIFormId 当 SerialId；多实例是否保存了自己的句柄 |
| 弹窗下方按钮仍能点击 | 遮挡 Graphic、Raycast Target、CanvasGroup、UICamera/EventSystem |
| 页面每次开一次回调多一次 | OnOpen 重复 AddListener/订阅，OnClose 漏解绑 |
| 重开显示旧内容、关闭后报错 | 数据只在 OnInit 设置、业务异步未取消、旧回调未失效 |
| Editor 正常、构建找不到 | GF 资源收集/构建产物，不只检查磁盘文件 |

## 4. 实现依据

[UIComponent](../../../../Plugins/UnityGameFramework/Scripts/Runtime/UI/UIComponent.cs)、[UIExtension](../../../../GameMain/Scripts/UI/Runtime/UIExtension.cs)、[UGuiForm](../../../../GameMain/Scripts/UI/Runtime/UGuiForm.cs)、[UGuiGroupHelper](../../../../GameMain/Scripts/UI/Runtime/UGuiGroupHelper.cs)、[AssetUtility](../../../../GameMain/Scripts/Utility/AssetUtility.cs)、[UI Panel Manager](../../../../GameMain/Editor/UIFormPanelGeneratorWindow.cs)、[SettingUIPanel](../../../../GameMain/Scripts/UI/Panel/SettingUIPanel.cs)、[GameLauncher](../../../../GameMain/Scenes/GameLauncher.unity)。

## 制作入口

[页面制作与注册](PageWorkflow.md) · [Prefab 规范](PrefabRules.md) · [复用目录](ReuseCatalog.md) · [验收清单](Validation.md)
