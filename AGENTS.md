# AGENTS.md

## 代码风格

- K&R 大括号风格（左大括号在行尾，不换行）
- 禁止 Allman 风格（左大括号单独一行）
- 优先复用 `Utils/` 文件夹中的工具类，避免重复造轮子
- 编写菜单界面时，必须使用项目自带的 UI 框架（`Framework/UI/`），包括 UiBaseMenu、UiButton、UiText、UiTable、UiRow/UiColumn 等组件，禁止直接使用 Stardew Valley 原版的 `IClickableMenu` 或自行绘制
- 配置界面使用 `IGenericModConfigMenuApi` 并通过 `Compatibility/Integrations.cs` 统一注册
- 禁止修改 `[CP]Red Panda Bazaar/` 文件夹下的任何文件（Content Patcher 内容包）
- 新的i18n条目必须放在对应的版本注释下
- 每次提交时，需提交所有已跟踪文件（即未被.gitignore标注的文件）