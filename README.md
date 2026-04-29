# Red Panda Bazaar — 星露谷物语 SMAPI 模组

星露谷物语扩展模组，新增 3 个 NPC、多个地图场景、商店和节日。需配合 Content Patcher 内容包使用。

## 功能一览

### 🚌 交通系统
- 巴士站新增入口，花费 300g 前往 `Custom_MapleBridge`
- 熊猫集市内售票站，可传送至鹈鹕镇、沙漠（500g）等
- 可选集成 Central Station 交通网络

### 🌸 春季集市（SpringFair）
- 春 8 自动举办，强制微风天气
- 内置小游戏：钓鱼、射击靶子、轮盘赌
- 兑奖机（经典版 + Joja 版），消耗兑奖券抽奖
- 背包扩容（24 / 36 格）

### 🐾 生物粒子效果
- 多个自定义地图中生成蝴蝶和萤火虫
- 白天出蝴蝶、黄昏消退、夜晚出萤火虫
- 雕像类物品附近也会吸引生物
- 可配置生物密度倍率

### 🍜 自定义 Buff
- **金色盛宴**：全属性大增强（速度、防御、攻击、运气等）
- **金色杯蛋糕**：运气 +6
- **多种冰棒**：不同属性组合（金色味、咖啡味、蕨菜味、芒果味、桃子味、南瓜味）
- **赌徒帽**：速度 +1
- **牛奶布丁**：食用后获得耕种经验

### 📋 特殊订单系统
- 自定义订单面板，每周一刷新
- 陈小明订单奖励系统

### 🪑 家具装饰
- Marlin 鱼店内放置不可移除的观赏鱼缸

### 🦋 萤火虫之夜
- 秋 11 自动触发，强制大风天气
- 傍晚全地图生成蝴蝶视觉效果

### ⚙️ GMCM 配置
- 生物密度倍率（×0.5 ~ ×2.0）
- 抽奖动画速度（×0.5 ~ ×5.0）

## 项目结构

```
Red Panda Bazaar Code/       # C# SMAPI 模组
├── ModEntry.cs               # 入口
├── manifest.json
├── Config/ModConfig.cs       # 配置
├── Constant/                 # 常量（物品ID、i18n键、技能ID等）
├── Custom/                   # 自定义 UI 和小游戏
│   ├── RPB_FishingGame.cs
│   ├── RPB_TargetGame.cs
│   ├── RPB_ClassicMachineMenu.cs
│   ├── RPB_JojaMachineMenu.cs
│   └── RPB_SpecialOrderBoard.cs
├── Handlers/                 # 功能模块
│   ├── TransportationHandler.cs
│   ├── CritterHandler.cs
│   ├── SpringFairHandler.cs
│   ├── BuffHandler.cs
│   ├── MenuHandler.cs
│   ├── SpecialOrdersHandler.cs
│   ├── FurnitureHandler.cs
│   └── BufferflyNightHandler.cs
├── Patches/                  # Harmony 补丁
│   ├── HarmonyPatch_CustomFishingGame.cs
│   ├── HarmonyPatch_CustomFoodEffects.cs
│   └── HarmonyPatch_CustomSpecialOrders.cs
├── Compatibility/            # 可选集成
│   ├── Integrations.cs       # GMCM + CentralStation
│   └── ModApi/
└── Utils/Tools.cs

[CP]Red Panda Bazaar/        # Content Patcher 内容包（美术资源）
```

## 构建

```bash
dotnet build "Red Panda Bazaar Code/Red Panda Bazaar Code.csproj"
```

自动编译 → 部署到 `Stardew Valley/Mods/`。

## 依赖

| 依赖 | 类型 | 说明 |
|---|---|---|
| SMAPI 4.0+ | 必装 | |
| `Lilaoliu.RedPandaBazaar` | 必装 | CP 内容包 |
| `spacechase0.GenericModConfigMenu` | 可选 | 配置界面 |
| `Pathoschild.CentralStation` | 可选 | 中央车站交通集成 |

## 版本号

格式 `1.x.y-Build.z`，`y` 为语义化版本号，`z` 为构建号（构建脚本自动递增）。

## 分支策略

- **master** — 稳定发布
- **develop** — 日常开发

## 安装

1. 安装 [SMAPI](https://smapi.io)
2. 安装 [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915)
3. 安装依赖的 CP 内容包
4. 将 `Red Panda Bazaar Code` 放入 `Mods/` 文件夹
5. 通过 SMAPI 启动游戏
