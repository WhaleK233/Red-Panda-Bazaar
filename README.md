# Red Panda Bazaar

《星露谷物语》SMAPI 扩展模组。在游戏内新增 NPC、场景地图、商店与节日，并提供银行、玩家摊位、共享单车、兑奖机等玩法系统。本模组依赖配套的 Content Patcher 内容包方可运行。

> 非中文环境需另行安装对应翻译文件。

## 技术栈

| 项 | 说明 |
|---|---|
| 运行时 | .NET 6（`net6.0`）—— 受 SMAPI 4.x 约束，不随上游升级 |
| 框架 | SMAPI 4.x |
| 补丁 | Harmony（5 个补丁类，共 11 处 `harmony.Patch` 调用） |
| UI | StardewUI.Core（外部共享源码项目，经 `Import` 在编译期注入） |
| 构建 | `dotnet` CLI / GitHub Actions，不依赖 Python |

## 仓库结构

```
RedPandaBazaar.sln               解决方案，含 mod / tests / tools 三个工程
Directory.Build.props            三工程共用的 MSBuild 属性
global.json                      固定的 .NET SDK 版本

src/RedPandaBazaar/              C# SMAPI 模组
├── ModEntry.cs                  唯一入口：ModInfo.Init → GameLaunched → FeatureInit() → HarmonyPatch()
├── manifest.json                模组元数据
├── Config/ModConfig.cs          GMCM 配置项
├── Constant/                    常量定义（I18nKeys / ItemsKeys / MPMessageType / StatsKeys 等）
├── Features/                    功能模块，一模块一目录
│   ├── Bank/                    银行与投资
│   ├── PlayerStall/             玩家摊位
│   ├── SpringFair/              春季展览会（含钓鱼与打靶小游戏）
│   ├── PrizeMachines/           兑奖机
│   ├── SpecialOrders/           特殊订单
│   ├── SharedBikes/             共享单车
│   ├── Transportation/          交通系统
│   ├── Critters/                氛围生物
│   ├── ButterflyNight/          秋 11 萤火虫之夜
│   ├── Buffs/                   自定义增益
│   └── Furniture/               家具装饰
├── Compatibility/               可选集成（GMCM / Central Station / Dev Debug Kit）
├── Utils/                       公共实现（ModInfo / MenuUi / Tools 等）
└── i18n/                        SMAPI 翻译文件

tests/RedPandaBazaar.Tests/      xUnit 测试：纯逻辑、仓库工具、仓库冒烟
tools/RedPandaBazaar.RepoTools/  仓库工具：validate / rc-version

content/[CP]Red Panda Bazaar/    Content Patcher 内容包（只读，含地图、NPC、商店、节日、物品定义与美术资源）
```

### 目录名与产物名的解耦

工程目录已按 `src / tests / tools / content` 分层，但**发布产物标识保持稳定**。csproj 显式声明：

```xml
<AssemblyName>Red Panda Bazaar Code</AssemblyName>
<ModFolderName>Red Panda Bazaar Code</ModFolderName>
```

二者分别决定程序集文件名与 `Mods/` 下的部署目录名，并与 `manifest.json` 的 `EntryDll` 绑定。该约束保证既有玩家升级时不会出现新旧目录并存所导致的重复 `UniqueID` 错误，仓库结构可继续演进而不影响安装布局。

## 构建

```bash
# 完整构建：编译 → 部署至 Mods/ → 打包 zip
dotnet build "src/RedPandaBazaar/RedPandaBazaar.csproj"

# 仅编译验证：跳过部署与打包，无副作用
dotnet build "src/RedPandaBazaar/RedPandaBazaar.csproj" -p:EnableModDeploy=false -p:EnableModZip=false
```

完整构建执行两项附加操作：

- **内容包部署**（`DeployCP`）：以 `content/[CP]Red Panda Bazaar/` 为源，先清空目标目录再全量复制到 `Stardew Valley/Mods/`。由 `EnableModDeploy=false` 关闭。
- **打包**：产物输出至 `src/RedPandaBazaar/out/net6.0/`，同时生成版本化的 zip。多次构建会在该目录累积历史包，可定期清理。由 `EnableModZip=false` 关闭。

构建过程不改写任何受版本控制的文件：版本号唯一来源为 csproj 的 `<Version>`，`manifest.json` 保留 `%ProjectVersion%` 占位符，由 ModBuildConfig 在打包阶段替换。

## 校验与测试

```bash
# 仓库一致性校验：JSON 可解析性、i18n 双文件键集合与顺序、
# I18nKeys.cs 双向对应、manifest 字段、csproj TFM 与版本号、内容包只读约束
dotnet run --project "tools/RedPandaBazaar.RepoTools" -- validate

# 单元测试与仓库冒烟测试
dotnet test "tests/RedPandaBazaar.Tests/RedPandaBazaar.Tests.csproj"
```

涉及游戏内行为的改动无法由自动化测试覆盖，交付前需在游戏中按验证清单逐项确认。

## 依赖

| 依赖 | 类型 | 用途 |
|---|---|---|
| SMAPI 4.0+ | 必需 | 模组加载器 |
| `Lilaoliu.RedPandaBazaar` | 必需 | Content Patcher 内容包 |
| `spacechase0.GenericModConfigMenu` | 可选 | 配置界面 |
| `Pathoschild.CentralStation` | 可选 | 中央车站交通集成 |

## 安装

1. 安装 [SMAPI](https://smapi.io) 与 [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915)。
2. 安装上表所列的必需依赖内容包。
3. 将 `Red Panda Bazaar Code` 与 `[CP]Red Panda Bazaar` 两个目录置于 `Mods/` 下。
4. 通过 SMAPI 启动游戏。

## 版本管理

版本号存在唯一来源：`src/RedPandaBazaar/RedPandaBazaar.csproj` 的 `<Version>`。`manifest.json` 通过 `%ProjectVersion%` 占位符引用该值，程序集版本同样由它派生。

| 场景 | 格式 | 生成方式 |
|---|---|---|
| 本地开发 | `x.y.z-Beta+<短 SHA>[-dirty]` | csproj 中手工维护 `x.y.z-Beta`；`AppendGitRevision` Target 在构建期追加 git 标识 |
| CD 预览版 | `x.y.z-rc.<日期>.<run>` | 由 `<Version>` 派生，CI 经 `-p:Version=` 注入，不写回仓库 |

本地构建的 git 标识取自 `git rev-parse --short HEAD`，并结合 `git status --porcelain` 判定工作区状态：干净时为 `1.2.0-Beta+04e0518`，存在未提交改动（含未跟踪文件）时为 `1.2.0-Beta+04e0518-dirty`。该标识同步体现在 zip 文件名、`manifest.json` 的版本号与程序集版本三处，可用于确认构建产物对应的源码状态。

按 semver 2.0，build metadata 不参与版本优先级比较，因此不会导致 SMAPI 将本地构建误判为新版本。

## 发布

向 `develop` 分支推送后，若仓库校验与编译检查均通过，CI 自动构建并刷新 [RC Release](https://github.com/WhaleK233/Red-Panda-Bazaar/releases/tag/rc)。

RC 为开发预览版，长期复用同一条 release（tag 固定为 `rc`），每次构建原地覆盖标题、说明与产物，确保已分发的下载链接始终有效。

## 分支策略

| 分支 | 用途 |
|---|---|
| `main` | 稳定发布 |
| `develop` | 日常开发，推送后自动发布 RC |
