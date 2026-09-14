# PSB To UGUI — 通用转换工具

从 PSB 文件一键生成像素级精准的 UGUI 预制体，符合 GameFramework 规范。

## 快速开始

### 1. 设计师准备

**PSB 文件要求：**
- 格式：.psb（Photoshop 大文档格式）
- 画布尺寸：与设计分辨率一致（如 1080x1920）
- 图层命名规范：`btn_` 前缀为按钮，`bg_` 为背景，`icon_` 为图标，`txt_` 为文字
- 图层分组：用 Photoshop 图层组组织层级
- 所有图层效果需栅格化（右键 → 栅格化图层样式）

**切图目录：**
在 PSB 文件同目录创建 `{PSB文件名}_sprites/` 文件夹，放入手动导出的 PNG：
```
签到.psb
签到_sprites/
├── bg_card.png         ← 全局共用图
├── day1/
│   └── bg_card.png     ← day1 专用图（同名优先）
├── day2/
│   └── bg_card.png
└── ...
```

### 2. 开发使用

1. 在 Unity 中选中 .psb 文件
2. 右键 → **PSB To UGUI**（或菜单 Tools → PSB To UGUI）
3. 在配置窗口确认面板名称和输出路径
4. 点击 **Convert**

### 3. 输出

- `Assets/GameMain/UI/UIPanel/{PanelName}.prefab` — UGUI 预制体（不含 Canvas）
- `Assets/GameMain/Scripts/UI/Panel/{PanelName}.cs` — C# 脚本（继承 UGuiForm）

## 命名约定

| PSB 图层前缀 | 含义 | Unity 组件 |
|-------------|------|-----------|
| `btn_` | 按钮 | Image + Button |
| `bg_` | 背景 | Image |
| `icon_` | 图标 | Image (preserveAspect) |
| `txt_` | 文字 | Image |
| 无前缀 | 装饰 | Image (raycast off) |

## 适配说明

- **有 GameFramework**: 脚本继承 `UGuiForm`，使用 `OnInit/OnClose` 生命周期
- **无 GameFramework**: 脚本继承 `MonoBehaviour`，使用 `Awake`
- **中文 PSB 文件名**: 自动翻译（签到→SignIn, 商店→Shop 等）
