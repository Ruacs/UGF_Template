# UI Prefab 制作规范

[文档导航](../../README.md) · [框架模块](../README.md)

职责、接入与限制依据当前实现；本文的验收项不代表本轮已执行运行验证。

## 1. UI Prefab 制作规范

### 1.1 根节点与推荐结构

优先参考模板或同类完成页面；下面的子节点名字是组织建议，不是框架强制 Find 路径。

```text
ExampleUIPanel                  RectTransform + 页面脚本（UGuiForm 派生）
├─ Background / InputBlocker    背景或全屏点击遮挡
└─ ContentRoot                 布局和动画内容根
   ├─ Header                   标题、关闭按钮
   ├─ Body                     主内容、ScrollRect、Widget
   └─ Footer                   确认等操作
```

- 页面逻辑挂在 **Prefab 根节点**，只保留一个实际页面逻辑，移除复制模板留下的另一套页面脚本；根节点使用 RectTransform、UI Layer、单位缩放和零旋转。
- 根 RectTransform 使用全屏拉伸、anchoredPosition/sizeDelta 为零。`UGuiForm.OnInit` 会强制这些值；弹窗大小设在 ContentRoot，不设在页面根节点。
- 基类运行时补 Canvas、CanvasGroup、GraphicRaycaster；模板已有的组件可以保留。GF Helper 运行时补 `UIForm`，无须再手工复制一套窗口管理器。
- 保持 Launcher 的 UI 根 Canvas、UICamera、CanvasScaler 和 EventSystem 配置；页面 Prefab 不再创建独立 EventSystem 或相机。当前基础 GF Prefab 参考分辨率为 `1080×2400`，具体运行值同时检查 Launcher overrides，不为做一个页面修改全局适配。
- 普通 Widget 不挂 UGuiForm，不加入 UIForm.txt；按需要用 MonoBehaviour 或现有 UIItemBase，生命周期由页面/列表池管理。

### 1.2 布局、滚动与点击

- 顶部/底部按钮按区域锚定，滚动区由 Viewport 与 Content 承担；列表内容根与布局组件职责保持一致，避免同时手动定位和 LayoutGroup 驱动同一轴。
- 长文案、空列表、最大数量、不同屏幕比例都要检查；安全区不能假定由 UGuiForm 自动处理，该基类没有安全区适配实现。
- 装饰图片和文本一般关闭 Raycast Target；按钮与需要拦截点击的背景保留命中目标。
- **模板根 Image 的 Raycast Target 当前为关闭。** CanvasGroup.blocksRaycasts 为 true 也不会凭空生成命中区域；模态弹窗要另设覆盖屏幕的可命中 Graphic，并验证背景按钮确实点不到。
- 层级、视觉遮罩与点击遮挡分别验证；透明 Image 可以阻挡点击，透明 CanvasGroup 不意味着自动禁止交互。
- ScrollRect 关闭/重开时恢复滚动位置和速度，确保弹出动画结束后布局正确。可参考 [LanguageUIPanel](../../../../GameMain/Scripts/UI/Panel/LanguageUIPanel.cs)。

### 1.3 引用、动画和复用

- `[SerializeField]` 直接绑定按钮、文本、列表容器和 Widget；必要字段不能用空值吞掉缺绑定。保存 Prefab 后重新打开检查引用，无 Missing Script/跨场景对象引用。
- 按钮用 Inspector UnityEvent 或代码绑定时避免对同一动作重复注册；不要对共享按钮随意 RemoveAllListeners 清掉其他拥有者的绑定。
- `m_openAnimation` / `m_closeAnimation` 可绑定现有 DOTweenSequence；无自定义动画时基类默认淡入淡出。模板的动画目标应在复制后指向新 Prefab 内的组件。
- 基类的反向开场关闭选项调用的是开场序列 DORewind 并配合淡出，不应理解为自动生成完整的逆向退场动画。
- 页面池复用时重置本次数据、按钮可点击状态、选中项、进度和业务动画；关闭后不要让旧回调刷新下一次打开的页面。

### 1.4 多语言与资源归属

- 静态 Text/TMP 文本添加 `UIStringKey`，填写实际字典 Key；动态文本由页面业务按 Key/参数刷新。
- 语言图片使用 `UILocalizedImageKey`，保证各语言同名资源与收集一致；完整文本/语言图片接入见 [本地化手册](../Localization/README.md)，字形和样式接入见 [TMPFont 手册](../TMPFont/README.md)。
- UGuiForm.InitLocalization 会遍历子 TMP_Text，若设置了主字体则替换字体并将 fontStyle 设为 Normal。特殊样式要验证刷新后的实际效果，不能只看编辑态。
- 专用 Widget、图标和配置跟随页面所属游戏/活动；公共复用资源才进入公共域。复制页面不应让新游戏无意依赖另一个游戏的私有资产。
