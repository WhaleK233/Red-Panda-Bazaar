# Red Panda Bazaar — 星露谷物语 SMAPI 模组

星露谷物语扩展模组，新增 NPC、地图场景、商店、节日，以及银行、玩家摊位、共享单车、兑奖机等玩法系统。需配合 Content Patcher 内容包使用。

> **注意：** 非中文玩家需安装对应翻译文件。

## 项目结构

```
RedPandaBazaar.sln           # 解决方案（src / tests / tools 三个工程）
Directory.Build.props        # 三工程共用的 MSBuild 属性
global.json                  # 钉住的 .NET SDK 版本

src/RedPandaBazaar/          # C# SMAPI 模组
├── ModEntry.cs              # 入口：ModInfo.Init → 订阅 GameLaunched → FeatureInit() → HarmonyPatch()
├── manifest.json            # 模组元数据
├── Config/ModConfig.cs      # GMCM 可配置项
├── Constant/                # 常量（I18nKeys, ItemsKeys, MPMessageType, PatchType 等）
├── Features/                # 功能模块，每个子目录一个独立功能
│   ├── Bank/                # 银行与投资
│   ├── PlayerStall/         # 玩家摊位系统
│   ├── SpringFair/          # 春季展览会（含钓鱼 / 打靶小游戏）
│   ├── PrizeMachines/       # 兑奖机
│   ├── SpecialOrders/       # 特殊订单
│   ├── SharedBikes/         # 共享单车
│   ├── Transportation/      # 交通系统
│   ├── Critters/            # 生物粒子效果
│   ├── ButterflyNight/      # 秋 11 萤火虫之夜
│   ├── Buffs/               # 自定义 Buff
│   └── Furniture/           # 家具装饰
├── Compatibility/           # 可选集成（GMCM、Central Station、Dev Debug Kit）
│   └── ModApi/              # 外部 Mod API 接口
├── Utils/                   # ModInfo / MenuUi / Tools 等公共实现
└── i18n/                    # SMAPI 翻译文件

tests/RedPandaBazaar.Tests/  # xUnit 测试（纯逻辑 + 仓库工具 + 仓库冒烟）
tools/RedPandaBazaar.RepoTools/  # 仓库工具：validate / rc-version

content/[CP]Red Panda Bazaar/    # Content Patcher 内容包：地图、NPC、商店、节日、物品定义与美术资源（本仓库只读）
```

> 工程目录已标准化，但**发布产物名保持不变**：csproj 用 `<AssemblyName>` 与 `<ModFolderName>` 把 DLL 名和 `Mods/` 部署目录钉为 `Red Panda Bazaar Code`，老玩家升级不会出现新旧目录并存。

## 构建

```bash
# 常规构建：编译 → 部署到 Mods/ → 打包 zip（有副作用，见下）
dotnet build "src/RedPandaBazaar/RedPandaBazaar.csproj"

# 仅编译检查：不部署、不打包，零副作用
dotnet build "src/RedPandaBazaar/RedPandaBazaar.csproj" -p:EnableModDeploy=false -p:EnableModZip=false
```

常规构建会把编译产物连同仓库内 `content/` 下的 `[CP]Red Panda Bazaar/` 一并部署到 `Stardew Valley/Mods/`（CP 包为全量覆盖，先删后拷），并在 `src/RedPandaBazaar/out/net6.0/` 下打包 zip；反复构建会在这里堆积历史 zip。只想验证能否编译时请用上面第二条命令。构建**不会改写任何仓库文件**。

## 校验与测试

```bash
# 仓库一致性校验：JSON、i18n 双文件、manifest、csproj、CP 只读约束
dotnet run --project "tools/RedPandaBazaar.RepoTools" -- validate

# 单元测试 + 仓库冒烟
dotnet test "tests/RedPandaBazaar.Tests/RedPandaBazaar.Tests.csproj"
```

## 依赖

| 依赖 | 类型 | 说明 |
|---|---|---|
| SMAPI 4.0+ | 必装 | |
| `Lilaoliu.RedPandaBazaar` | 必装 | CP 内容包 |
| `spacechase0.GenericModConfigMenu` | 可选 | 配置界面 |
| `Pathoschild.CentralStation` | 可选 | 中央车站交通集成 |

## 安装

1. 安装 [SMAPI](https://smapi.io)
2. 安装 [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915)
3. 安装依赖的 CP 内容包
4. 将 `Red Panda Bazaar Code` 和 `[CP]Red Panda Bazaar` 放入 `Mods/`
5. 通过 SMAPI 启动游戏

## 版本号

版本号只有一个来源：`src/RedPandaBazaar/RedPandaBazaar.csproj` 的 `<Version>`。`manifest.json` 写 `%ProjectVersion%` 占位符，由 ModBuildConfig 在打包时替换为实际值；程序集版本同样由它派生。

| 场景 | 格式 | 说明 |
|---|---|---|
| 本地开发 | `x.y.z-Beta+<短SHA>` | 手动维护 csproj 中的 `x.y.z-Beta`；构建时自动追加 git 标识，不改写任何文件 |
| CD 预览版 | `x.y.z-rc.<日期>.<run>` | 由 `<Version>` 派生，CI 经 `-p:Version=` 注入，不写回仓库 |

本地构建的 git 标识来自 `git rev-parse --short HEAD`：工作区干净时是 `1.2.0-Beta+04e0518`，有未提交改动（含新增未跟踪文件）时是 `1.2.0-Beta+04e0518-dirty`。zip 文件名、`manifest.json` 的版本号、程序集版本三处同步带上该标识，据此可确认发给别人的包对应哪份代码。

## 发布

推送到 `develop` 后，仓库一致性校验与编译检查通过即自动构建并刷新 **[RC Release](https://github.com/WhaleK233/Red-Panda-Bazaar/releases/tag/rc)**。

RC 是开发预览版，长期复用**同一条** release（tag 固定 `rc`），下载链接恒定不变，每次构建被最新产物覆盖。

## 分支策略

- **main** — 稳定发布
- **develop** — 日常开发（推送后自动发布 RC）
