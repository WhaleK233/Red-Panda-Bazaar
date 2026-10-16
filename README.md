# Red Panda Bazaar — 星露谷物语 SMAPI 模组

星露谷物语扩展模组，新增 NPC、地图场景、商店和节日。需配合 Content Patcher 内容包使用。

> **注意：** 非中文玩家需安装对应翻译文件。

## 项目结构

```
Red Panda Bazaar Code/       # C# SMAPI 模组
├── ModEntry.cs               # 入口：Tools.Init → FeatureInit() → HarmonyPatch()
├── manifest.json             # 模组元数据
├── Config/ModConfig.cs       # GMCM 可配置项
├── Constant/                 # 常量（I18nKeys, DataKeys, ItemsKeys 等）
├── Features/                 # 功能模块，每个子目录一个独立功能
│   ├── Transportation/       # 交通系统
│   ├── SpringFair/           # 春季集市
│   ├── Critters/             # 生物粒子效果
│   ├── ButterflyNight/       # 秋 11 萤火虫之夜
│   ├── FishingMiniGame/      # 钓鱼小游戏
│   ├── TargetMiniGame/       # 射击靶子小游戏
│   ├── PrizeMachines/        # 兑奖机
│   ├── SpecialOrders/        # 特殊订单
│   ├── Buffs/                # 自定义 Buff
│   ├── Furniture/            # 家具装饰
│   └── PlayerStall/          # 玩家摊位系统
├── Compatibility/            # 可选集成（GMCM、Central Station 等）
│   └── ModApi/               # 外部 Mod API 接口
├── Utils/Tools.cs            # 全局工具类
└── i18n/                     # SMAPI 翻译文件

[CP]Red Panda Bazaar/        # Content Patcher 内容包（美术资源）
```

## 构建

```bash
dotnet build "Red Panda Bazaar Code/Red Panda Bazaar Code.csproj"
```

自动编译并部署到 `Stardew Valley/Mods/`。

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

版本号只有一个来源：`Red Panda Bazaar Code.csproj` 的 `<Version>`。`manifest.json` 写 `%ProjectVersion%` 占位符，由 ModBuildConfig 在打包时替换为实际值；程序集版本同样由它派生。

| 场景 | 格式 | 说明 |
|---|---|---|
| 本地开发 | `x.y.z-beta` | 手动维护在 csproj；构建不改写任何文件 |
| CD 预览版 | `x.y.z-rc.<日期>.<run>` | 由 `<Version>` 派生，CI 经 `-p:Version=` 注入，不写回仓库 |

## 发布

推送到 `develop` 后，仓库一致性校验与编译检查通过即自动构建并刷新 **[RC Release](https://github.com/WhaleK233/Red-Panda-Bazaar/releases/tag/rc)**。

RC 是开发预览版，长期复用**同一条** release（tag 固定 `rc`），下载链接恒定不变，每次构建被最新产物覆盖。

## 分支策略

- **main** — 稳定发布
- **develop** — 日常开发（推送后自动发布 RC）
