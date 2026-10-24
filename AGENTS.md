# Repository Guidelines

「Red Panda Bazaar（小熊猫集市）」——《星露谷物语》SMAPI 模组仓库的协作指南，供 AI 助手与协作者使用。

**工作语言：简体中文。** 注释、提交信息、文档一律中文；代码、标识符、路径、命令、报错原文保持原样。

---

## Project Overview

模组（Nexus 30982）由两个可独立安装的组件构成，二者通过地图名 / Tile Action 名 / 物品 ID 字符串硬耦合：

| 组件 | 目录 | 承担 |
|---|---|---|
| C# 模组 | `src/RedPandaBazaar/` | 全部玩法逻辑：银行、玩家摊位、春季展览会与其小游戏、特殊订单、交通、共享单车、抽奖机、菜单界面 |
| Content Patcher 内容包 | `content/[CP]Red Panda Bazaar/` | 地图 TMX、NPC、商店、节日、邮件、物品定义与全部美术资源 |

C# 侧 `manifest.json` 把 CP 包 `Lilaoliu.RedPandaBazaar` 标为 `IsRequired: true`，缺失会硬报错。CP 包由他人维护，**本仓库只读**；CP 更新不随构建自动同步，需手动处理。

技术栈：C# / `net6.0` / SMAPI 4.x / Harmony（可选 GMCM、Central Station、Dev Debug Kit 软依赖）。构建 / 校验 / 发布全链路只依赖 `dotnet`，不使用 Python。

**UI 框架是 StardewUI.Core**（`Projects/StardewUI/Core`，仓库外，共享源码项目，命名空间 `StardewUI.*`，经 csproj 的 `<Import>` 编译期注入）：菜单基类 `ViewMenu`，构件与布局在 `StardewUI.Widgets` / `StardewUI.Layout`，`Core/ViewMenu.cs:784` 的 `update` 之类钩子在子类里重写。

本 mod 的通用实现位于 `Utils/`（`Tools`、`ModInfo`、`MenuUi`、`ItemSerializer`、`RandomUtils`、`TimeUtils`）；`PatchType` 等常量类位于 `Constant/`；第三方 API 接口声明位于 `Compatibility/ModApi/`。

---

## Repository Layout

仓库采用 `src / tests / tools / content` 四层标准布局：可执行代码在 `src`，测试在 `tests`，仓库工具在 `tools`，只读内容包在 `content`。工程目录名与产物名已解耦（见本节末），因此目录调整不影响玩家。

```
RedPandaBazaar.sln                 # 解决方案（src / tests / tools 三个工程）
Directory.Build.props              # 三工程共用的 MSBuild 属性（TFM 与 Version 不在此处，见下）
global.json                        # 钉住的 .NET SDK 版本
AGENTS.md                          # 本文件（协作契约，置于仓库根）
src/RedPandaBazaar/                # C# SMAPI 模组
├── RedPandaBazaar.csproj          # 工程名已标准化；AssemblyName/ModFolderName 钉为旧名
├── ModEntry.cs                    # 唯一入口
├── manifest.json                  # Version 为 %ProjectVersion% 占位符
├── Config/ModConfig.cs            # 2 个 GMCM 配置项
├── Constant/                      # I18nKeys / ItemsKeys / MPMessageType / StatsKeys /
│                                  #   QuestsKeys / SkillsKeys / DataKeys / PatchType
├── Features/                      # 11 个功能模块，一模块一目录
├── Utils/                         # ModInfo / MenuUi / Tools / ItemSerializer / RandomUtils / TimeUtils
├── Compatibility/                 # Integrations.cs + ModApi/（第三方 API 接口声明）
├── DeBug/Debug.cs                 # 专属调试 UI 开关，转发 Dev Debug Kit API
├── docs/                          # StardewUI API 速查、春季展览会小游戏方案
├── i18n/                          # default.json（英）+ zh.json（中），必须同步维护
└── assets/                        # 6 个纹理 PNG + 2 个 Ogg 音效（共享单车）
tests/RedPandaBazaar.Tests/        # xUnit 测试工程（链接编译 mod 的纯逻辑源文件）
tools/RedPandaBazaar.RepoTools/    # 仓库工具控制台工程（纯 net6.0）：validate / rc-version
content/[CP]Red Panda Bazaar/      # 只读：Content Patcher 内容包
.github/workflows/ci.yml           # CI/CD 入口
.github/actions/setup-build/       # 复合 action：SDK + 游戏参考程序集 + StardewUI，CI 与 CD 共用
```

**目录与产物名已解耦**：工程目录标准化为 `src/RedPandaBazaar`，但 csproj 显式钉了 `<AssemblyName>Red Panda Bazaar Code</AssemblyName>` 与 `<ModFolderName>Red Panda Bazaar Code</ModFolderName>`，因此 DLL 名、`Mods/` 部署目录、release zip 名全部保持旧名。**不要改这两行**——改了会让老玩家的旧目录与新目录并存，SMAPI 报重复 UniqueID。同理 `manifest.json` 的 `EntryDll` 与它们绑定，由 RepoTools 校验。

`Directory.Build.props` 只放三工程共用的编译属性。`<TargetFramework>` 与 `<Version>` **必须留在 mod 的 csproj 文本里**：`RepoValidator.CheckCsproj` 与 `RcVersion.ReadBaseVersion` 都直接对该文件做正则匹配，抽到公共 props 会让 `validate` 报「未找到」。

---

## Architecture & Data Flow

### 启动链（`ModEntry.cs`）

```mermaid
flowchart TD
  E["ModEntry.Entry"] --> T["ModInfo.Init<br/>注入 Helper/Config/Monitor/I18n/Manifest"]
  T --> U["StardewUI.UI.Initialize<br/>初始化 UI 框架"]
  U --> CT["SharedBikes.RegisterContent<br/>注册贴图 / 音效资产"]
  CT --> GL["订阅 GameLoop.GameLaunched"]
  GL --> OGL["OnGameLaunched"]
  OGL --> I["Integrations.Init<br/>GMCM + CentralStation + Dev Debug Kit"]
  I --> F["FeatureInit × 11"]
  F --> H["HarmonyPatch<br/>new Harmony(UniqueID)"]
```

- `Entry` 只做 `ModInfo.Init` + `StardewUI.UI.Initialize` + `SharedBikes.RegisterContent` + 订阅 `GameLaunched`，**不要**在此添加其它逻辑。
- `StardewUI.UI.Initialize(helper, Monitor)` 必须先于任何菜单构造（Core 内部要注入 Helper 并订阅 `GameLoop` 事件）；漏掉会在打开菜单时抛 `InvalidOperationException`。
- `SharedBikes.RegisterContent()`（订阅 `Content.AssetRequested`）也必须在这里调用：游戏启动早期就会加载 `Data/AudioChanges`，订阅晚了的 `e.Edit` 不会执行（CP 同样在 Entry 订阅该事件）。这是「Entry 不放业务逻辑」的唯一例外，新增同类需求照此办理。
- `FeatureInit()` 按 `Bank → Buffs → Critter → Furniture → SpringFair → PlayerStall → PrizeMachines → SpecialOrders → ButterflyNight → Transportation → SharedBikes` 顺序调用各 Feature 的 `Init()`；新功能在末尾追加。
- `HarmonyPatch()` 依次 `SpringFairPatch → BuffsPatch → SpecialOrdersPatch → FurniturePatch → SharedBikesPatch`。

### 核心机制

| 机制 | 说明 |
|---|---|
| **`ModInfo` + `Utils.Tools`** | `Utils/ModInfo.cs` 存本 mod 专属信息（Helper/ModConfig/Monitor/I18n/ModManifest），启动时注入本地工具运行时；通用函数（日志 `Log*`、翻译 `GetI18n`、扣款 `TryCharge`、多人 `SendToHostOrBroadcast`、`IsGoodWeather`/`IsValidButtonAction`）集中在 `Utils/Tools.cs`。**不要**新增第二个服务定位器，也不要往业务类里传 `IModHelper` |
| **Feature 自治** | `Features/<Name>/` 每个目录一个独立功能，暴露 `public static void Init()`，自行订阅事件、注册 Tile Action、持有自己的 `*Data.cs` 与菜单。需要向游戏内容管线注册资产的功能（现仅 `SharedBikes`）额外暴露 `RegisterContent()`，由 `Entry` 调用 |
| **主机权威多人模型** | 改世界 / 改金币 / 结算只由 `Context.IsMainPlayer` 执行；客机发请求或收广播。消息类型集中在 `Constant/MPMessageType.cs` |
| **事件优于补丁** | 决策阶梯：`Data/` 表 → SMAPI 事件 → 已有 API → 扩展点 → Prefix/Postfix → Transpiler（最后手段）。目前全仓仅 5 个补丁类共 11 处 `harmony.Patch` 调用（SpringFair 4、Buffs 2、Furniture 3、SpecialOrders 1——CheckCompletion 同时挂 Prefix+Postfix、SharedBikes 1） |

### 数据流

```
玩家交互 → SMAPI 事件 / Tile Action → Feature 静态类
  → 修改内存状态（ModInfo.Helper.Data.ReadSaveData 得到的内存对象）
  → GameLoop.Saving 时落盘（仅主机）
  → 变更如需同步，Tools.SendToHostOrBroadcast 广播 / 定向发送
```

---

## Development Commands

> ⚠️ **构建有副作用，动手前务必读本节。**

```bash
# 常规构建（部署到 Mods + 打包 zip，见下表）
dotnet build "RedPandaBazaar.sln"

# 仅编译检查（不部署、不打包，零副作用）
dotnet build "src/RedPandaBazaar/RedPandaBazaar.csproj" -p:EnableModDeploy=false -p:EnableModZip=false
```

| 副作用 | 触发时机 | 说明 |
|---|---|---|
| 重新部署 CP 包到 `Mods/`（`DeployCP`） | BeforeBuild | 以仓库内 `content/[CP]Red Panda Bazaar/` 为源，先 `rmdir /s /q` 再全量拷贝；`EnableModDeploy=false` 时跳过 |
| 编译产物 → `src/RedPandaBazaar/out\` + 打包 zip | Build | zip 名绑定构建版本，反复构建会在 `src/RedPandaBazaar/out/net6.0/` 堆积；`EnableModZip=false` 时只产出 dll |

**构建不会改写任何仓库文件**：版本号只来自 csproj `<Version>`，`manifest.json` 写 `%ProjectVersion%` 占位符，由 ModBuildConfig 在打包时替换。只想验证编译时传入 `-p:EnableModDeploy=false -p:EnableModZip=false` 即可做到零副作用。CI 的编译检查正是这么跑的（见「Testing & QA」）。

产物路径：`src/RedPandaBazaar/out/net6.0/Red Panda Bazaar Code.dll`（文件名来自 `AssemblyName`，非工程名）。

**本地构建会自动带上来源代码标识**：`AppendGitRevision` Target 在构建期用 `git rev-parse --short HEAD` 取短 SHA，配合 `git status --porcelain` 判断是否有未提交改动（含新增未跟踪文件），把结果作为 semver build metadata 追加到 `<Version>`：干净工作区是 `1.2.0-Beta+<短SHA>`，有改动是 `1.2.0-Beta+<短SHA>-dirty`。zip 名、`manifest.json` 的版本号、程序集版本三处同步带标识。

- build metadata 不参与版本优先级比较（semver 2.0），因此不会被 SMAPI 误判为新版本。
- 该 Target 只在 `<Version>` 不含 `+` 时生效，CI 经 `-p:Version=` 注入 RC 版本号时用 `-p:AppendGitRevision=false` 显式关闭（build metadata 的 `+` 在下载 URL 中会被解码成空格）。
- 非 git 工作副本（如导出的源码包）里 git 命令会输出错误信息，SHA 用正则提取校验，取不到时版本号安全退回 csproj 中的值。

---

## Code Conventions & Common Patterns

### 硬性规则

- **优先复用 `Utils/` 中的工具类**，避免重复造轮子。
- **菜单界面必须使用 StardewUI.Core**：菜单继承 `ViewMenu` 并实现 `CreateView()`，窗口与构件走 `Utils/MenuUi.cs` 的工厂方法；**禁止**直接使用星露谷原版的 `IClickableMenu`，也不要自造布局系统（确需自绘时继承 `StardewUI.View`，重写 `OnMeasure` / `OnDrawContent`）。
  *已知例外（勿扩散）：`PlayerStallMenu : MenuWithInventory`（需背包交互）、`ClassicPrizeMachineMenu` / `JojaPrizeMachineMenu : IClickableMenu`、`SpecialOrderBoard : SpecialOrdersBoard`。*
- **配置界面使用 `IGenericModConfigMenuApi`**，并通过 `Compatibility/Integrations.cs` 统一注册。
- **禁止修改 `content/[CP]Red Panda Bazaar/` 下的任何文件**。它由他人维护，上游更新会整目录覆盖本地改动。
- **新的 i18n 条目必须放在对应的版本注释下**，不能直接追加到文件末尾。
- 每次提交时，需提交所有已跟踪文件（即未被 `.gitignore` 标注的文件）。

### 新增功能的标准姿势

1. 在 `Features/<Name>/` 新建静态类，暴露 `public static void Init()`，首尾各一行 `Tools.Log("<Name> Initializing.")` / `Inited.`。
2. 在 `ModEntry.FeatureInit()` 末尾追加调用。
3. 面向玩家的文本走 `Tools.GetI18n(I18nKeys.X)`；常量加进 `Constant/I18nKeys.cs` 对应 `#region`。
4. 物品 / 机器 / 家具 ID 加进 `Constant/ItemsKeys.cs` 的嵌套类，不要散落字符串。

### Harmony 补丁模板

五个补丁类（`Features/*/XxxPatch.cs`）形态完全一致，照抄：

```csharp
public static class XxxPatch {
    private const string NAME = nameof(XxxPatch);

    public static void ApplyPatch(Harmony harmony) {
        Tools.LogPatch(NAME, "Type.method()", PatchType.Prefix);
        harmony.Patch(
            original: AccessTools.Method(typeof(Type), nameof(Type.method)),   // 非公开方法用字符串名
            prefix: new HarmonyMethod(typeof(XxxPatch), nameof(Prefix_Type_method))
        );
    }

    private static bool Prefix_Type_method(Type __instance) {
        try {
            /* 先判身份：不是自己管的对象立刻 return true 放行 */
            return true;
        } catch (Exception e) {
            Tools.LogPatchErr(NAME, e);
            return true;   // 异常时放行，绝不阻断原方法
        }
    }
}
```

- 方法命名 `Prefix_{Type}_{method}` / `Postfix_{Type}_{method}`，补丁方法一律 `private static`。
- 被重写遮蔽时改补父类，并写注释说明（见 `FurniturePatch.cs`，`clicked` 补 `Furniture` 基类、`canBeRemoved` 补 `StorageFurniture` 基类）。
- 补丁参数名必须与原方法**完全一致**；用 `AccessTools.Method(type, name, new[] { typeof(...) })` 精确定位重载。

### 多人同步

```csharp
// 发送：主机广播全部，客机定向发主机
Tools.SendToHostOrBroadcast(data, MPMessageType.PlayerStall_AddItem);

// 接收
private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e) {
    if (e.FromModID != Tools.ModManifest.UniqueID) return;
    switch (e.Type) {
        case MPMessageType.PlayerStall_SyncData:
            if (Context.IsMainPlayer) break;
            Data = e.ReadAs<StallSaveData>();
            break;
    }
}
```

- 新消息类型加进 `Constant/MPMessageType.cs` 并按模块注释分组。
- 定向回复用 `playerIDs: new[] { e.FromPlayerID }`。
- **`PerScreen<T>` 只用于按屏隔离的纯表现状态**（如 `Critters` 的粒子计数），世界状态用主机权威 + 广播。

### 存档与状态

| 动作 | 判据 | 位置 |
|---|---|---|
| 读存档 | `Context.IsMainPlayer` | `GameLoop.SaveLoaded` |
| 写存档 | `Context.IsMainPlayer` | `GameLoop.Saving` |
| 改世界 / 结算 | `Context.IsMainPlayer` | 各 Feature 的事件处理器 |

- 判据区别：`IsMainPlayer` = 屏幕 0 上的权威玩家；`IsOnHostComputer` = 存档在本机（分屏第二玩家为真）。**访问存档 API 用 `IsOnHostComputer` 才不抛 `InvalidOperationException`，但 SaveLoaded 里的「重置 + 读存档」必须用 `IsMainPlayer` 并先放行分屏中途加入**（`IsSplitScreen && !IsMainPlayer` 直接 return）：分屏加入会在本机再次触发 SaveLoaded，静态 `Data` 是进程内共享的当日最新状态，重置或重读都会回滚到上次存档。
- 「是否主机」处理多人消息时用**进程级**的 `Game1.IsMasterGame`，不要用屏级的 `Context.IsMainPlayer`：混合联机（主机+分屏+远端客机）下，消息可能在主机进程的任意屏上下文触发。
- **同机分屏不走网络消息**：分屏客机定向发给 `MasterPlayer` 的消息会被 SMAPI 静默丢弃（master 不在 `Peers`，`sendToSelf` 只认当前屏玩家）。分屏与主机共享进程内静态数据，直接调用主机执行方法即可；需要以主机身份广播给远端客机时，经 `PlayerStall.HostScreenRelay` 这类「主机屏代发队列」转交（远端客机对广播的信任检查只认主机玩家）。
- 存档 key 常量集中在 `Constant/StatsKeys.cs`，前缀 `WhaleK233.RedPandaBazaar.`。
- `OnSaveLoaded` 中先 `Data = new T()` 重置跨存档状态，再按判据读取。
- **跨存档 / 跨网络传物品一律走 `Utils.ItemSerializer` 的 XML 快照**（`ItemSnapshot` 模式），数量以快照的 `Item.Stack` 为准，不要另存数量字段；惰性访问器写成**方法**（`GetItem()`）而非属性，否则会被 JSON 序列化器写回存档。

### UI 框架

```csharp
public class MyMenu : ViewMenu
{
    private Lane? _content;   // 需要按数据重建时保留引用（ViewMenu 没有 Rebuild）

    protected override IView CreateView()
    {
        CloseButtonSprite = UiSprites.CloseButton;   // 右上角关闭按钮
        _content = MenuUi.Column();                  // 宽度撑满内容区、高度自适应
        RebuildContent();
        return MenuUi.Window(_content);              // 菜单底色 + 边框，尺寸随内容自适应
    }

    private void RebuildContent()
    {
        if (_content is null) return;
        _content.Children.Clear();                   // 数据变了就整体重建，视图不持有状态

        _content.Children.Add(MenuUi.Header("标题"));

        var buttonRow = MenuUi.Row(topMargin: MenuUi.RowGap);
        buttonRow.Children.Add(MenuUi.PrimaryButton("确定", Close));
        _content.Children.Add(buttonRow);
    }
}
```

- `CreateView()` 在 `ViewMenu` 构造期被调用。C# 先执行实例字段初始化器、再调用基类构造函数，所以派生类字段此时已可用，但派生类**构造函数体**尚未执行——视图相关初始化（`CloseButtonSprite`、事件订阅、控件引用）写在 `CreateView` 里最稳。
- 重建视图树 = `Children.Clear()` + 重新 `Add`；`ViewMenu` 每帧 `MeasureAndCenterView()` 会自动重测并重新居中，尺寸变化无需手动处理。
- 行内元素间距用 `Margin`（`Lane` **没有** `Spacing`/`ItemSpacing` 属性）；列内行间距用 `MenuUi.Row(..., topMargin)`，常规行距取 `MenuUi.RowGap`；"文本左 + 控件贴右端"的行用 `MenuUi.RowBetween`（左侧文本设 `Length.Stretch()` 吃掉剩余宽度）；长列表用 `ScrollableView`（自身尺寸须固定）。
- 文本自动换行始终开启，水平对齐用 `Label.HorizontalAlignment`（宽度需大于文本）；`Button` 默认参与手柄焦点导航。
- `MenuUi` 是本 mod 菜单的唯一视觉设计系统：外层使用 `Window`，页面标题使用 `Header`，相关信息使用 `Card` 分组；操作按钮按重要性使用 `PrimaryButton` / `SecondaryButton`，不要在菜单中散落背景贴图、颜色和间距常量。
- 数据型页面必须区分信息层级：关键数值使用较大或加粗文本，提示与单位使用 `MenuUi.Muted`；状态颜色只表达正向、警告或锁定含义，不把颜色当作唯一信息来源。
- 表格使用 `Grid` 的 `GridItemLayout.Count` 配合响应式 `Length.Stretch()`，表头与数据行共用同一 Grid；长列表放进有固定高度的 `ScrollableView`，禁止用固定像素列坐标模拟表格。
- 重建列表时只重建数据区域；实时变化的数值保留 `Label` 引用更新，避免每帧清空视图树导致焦点和输入状态丢失。
- 自绘内容必须包在 `Frame` / `Card` 外壳内，并按视口计算尺寸；不得让自绘控件自行决定整个菜单的外观和关闭逻辑。

写新菜单前先查 `docs/StardewUI-Core-Api-Cheatsheet.md`（本 checkout 版 StardewUI.Core 的纯代码 API 速查，含"不存在"清单与源码行号）。

### i18n

```json
// i18n/default.json 与 i18n/zh.json —— 同键、同序、占位符同名
{
  /****** Version 1.2.0 Added ******/
  "PlayerStall.TotalTax": "Total Tax: {{amount}}{{gold}}",
```

```csharp
public const string PlayerStall_TotalTax = "PlayerStall.TotalTax";   // Constant/I18nKeys.cs
var msg = Tools.GetI18n(I18nKeys.PlayerStall_TotalTax)
    .Tokens(new { amount = totalTax, gold }).ToString();
```

- 文件顶部是**倒序版本块**（最新在上），新键挂进对应版本块；无新增时新建一块置于最上方。
- **删除键**要在发生删除的版本块内留一行履历：`/****** Delete "Key1", "Key2" ******/`（键名保持英文），再从两份 JSON 与 `I18nKeys.cs` 中移除。现存的 Bank、Tetris 删除行即是示例。
- 占位符名（`{{amount}}`、`{{gold}}`、`{{rate}}`）必须与 `.Tokens(new { ... })` 的匿名属性名一致。
- 两份 JSON 的键集合与顺序必须逐行一致；文件含 `/* */` 注释属正常，SMAPI 可解析。

---

## Important Files

| 文件 | 为什么重要 |
|---|---|
| `src/RedPandaBazaar/ModEntry.cs` | 唯一入口；启动链与补丁注册点 |
| `src/RedPandaBazaar/Utils/ModInfo.cs` | 本 mod 专属信息（Helper/配置/Monitor/翻译/清单），启动时注入 `Utils.Tools` |
| `src/RedPandaBazaar/Utils/MenuUi.cs` | StardewUI 构件工厂（窗口/行/列/文本/按钮/分隔线），所有菜单共用 |
| `Projects/StardewUI/Core/ViewMenu.cs`（仓库外） | 菜单基类：输入分发、焦点导航、每帧测量与居中都在这里 |
| `src/RedPandaBazaar/Utils/ItemSerializer.cs` | 物品 XML 快照；跨存档/网络传物品的唯一正确途径 |
| `src/RedPandaBazaar/Compatibility/Integrations.cs` | GMCM 配置注册与 Central Station 集成，可选依赖的统一入口 |
| `src/RedPandaBazaar/Constant/MPMessageType.cs` | 多人消息类型常量 |
| `src/RedPandaBazaar/Constant/StatsKeys.cs` | 存档 key 与游戏统计 key |
| `src/RedPandaBazaar/RedPandaBazaar.csproj` | 版本号唯一来源（`<Version>`）、产物名钉定（`<AssemblyName>` / `<ModFolderName>`）与两个自定义 Target（`DeployCP`、`AppendGitRevision`）都在这里，改动前先读「Development Commands」 |
| `src/RedPandaBazaar/manifest.json` | `Version` 为 `%ProjectVersion%` 占位符，实际值由 csproj `<Version>` 在打包时替换 |
| `tools/RedPandaBazaar.RepoTools/` | 仓库工具（validate / rc-version）：校验逻辑在 `RepoValidator.cs`，JSONC 解析在 `Jsonc.cs`，RC 版本派生在 `RcVersion.cs` |
| `.github/workflows/ci.yml` | CI/CD 入口：`validate` 跑 RepoTools 校验与单元测试；`build` 用参考程序集在 Linux 上编译；`release` 在校验与编译都通过后发布 RC |
| `.github/actions/setup-build/action.yml` | 复合 action：安装 .NET SDK、游戏参考程序集与 StardewUI，CI 与 CD 共用 |
| `src/RedPandaBazaar/i18n/default.json` | 翻译基准，结构即规范 |
| `content/[CP]Red Panda Bazaar/content.json` | CP 侧入口：`CustomLocations` 注册地图、`Changes` 拼装数据 |

---

## Runtime/Tooling Preferences

| 项 | 要求 |
|---|---|
| 运行时 / TFM | **`net6.0`，不要升级**（SMAPI 4.x 本身即 net6.0）；EOL 提示已用 `CheckEolTargetFramework=false` 关闭 |
| 构建工具 | `dotnet` CLI 或 Rider / Visual Studio |
| 构建助手 | `Pathoschild.Stardew.ModBuildConfig` 4.4.0（唯一 NuGet 依赖），自动探测游戏目录并引用游戏与 SMAPI 程序集，含 `0Harmony.dll`（因 `EnableHarmony=true`） |
| **GitHub Actions 权限** | `ci.yml` 顶层 `contents: read`；仅 `release` job 单独提权到 `contents: write`，用于创建 / 刷新 Release |
| **`Projects/StardewUI/Core`** | StardewUI.Core 源码，csproj 的 `StardewUICorePath` 默认 `$(RepoRoot)..\..\..\Projects\StardewUI\Core`（从仓库根向上三级，绝对化后不受工程目录深度影响），可用同名属性覆盖；缺失则构建硬失败。CI 钉在 `Mushymato/StardewUI` 的 `dev-unofficial` 分支固定提交 `355eefc`，升级本地框架检出时同步更新该 ref |
| **C# 语言版本 / Newtonsoft** | StardewUI 源码用了 C# 12+ 语法，`LangVersion` / `ImplicitUsings` / `Nullable` / `CheckEolTargetFramework` 统一在根目录 `Directory.Build.props` 设置（`<TargetFramework>` 与 `<Version>` 例外，必须留在 mod 的 csproj 里供校验器读取）；其 Data 解析依赖 SMAPI 内置的 `$(GamePath)\smapi-internal\Newtonsoft.Json.dll`（csproj 里显式 `<Reference>`），二者都不要删 |
| **外部源码告警** | StardewUI 的 CS9336 冗余模式告警经 csproj `NoWarn` 豁免——钉住的外部共享源码不在本仓库修复范围 |
| 游戏安装 | 本地构建必须能被 ModBuildConfig 自动探测到（注册表 / 常见路径），否则构建报错 |
| 外部源 | NuGet 需可还原 `Pathoschild.Stardew.ModBuildConfig`；离线时首次构建可能失败 |

版本号只写在 `src/RedPandaBazaar/RedPandaBazaar.csproj` 的 `<Version>`（本地开发格式 `x.y.z-Beta`）。`manifest.json` 的 `Version` 写 `%ProjectVersion%` 占位符，ModBuildConfig 打包时替换为实际值；程序集版本也由它派生。本地构建由 `AppendGitRevision` Target 追加 `+<短SHA>[-dirty]`（见「Development Commands」）。CD 发布的 RC 包版本为 `x.y.z-rc.<日期>.<run>`（run = GitHub Actions 运行号），由 RepoTools 的 `rc-version` 子命令派生后经 `-p:Version=` 注入构建，不写回仓库。构建过程**不会改写任何仓库文件**。

---

## Testing & QA

**自动化测试是 xUnit（`tests/RedPandaBazaar.Tests/`），测试对象分两块**：

- **仓库工具**：`tools/RedPandaBazaar.RepoTools`（纯 net6.0 控制台工程）的 JSONC 解析与 RC 版本派生；工程引用直接测内部函数。
- **仓库冒烟**：对当前检出副本运行完整校验（`RepoValidator.Run`，静默模式断言退出码）。

需要测 mod 的纯游戏逻辑时，以 `<Compile Include>` 链接方式直接编译该源文件（游戏状态由调用方注入，不引用游戏程序集），不要引用 mod 工程本身——引用会连带触发 mod 构建与部署副作用。文件一旦引入游戏依赖，链接编译会立刻失败，这是「该文件不再可测」的信号，把文件移出列表而不是给测试工程补游戏引用。

仓库一致性校验（静态检查）与单元测试的本地入口：

```bash
# 校验：JSON 可解析性与重复键、i18n 双文件键集合 / 顺序 / 占位符、
# I18nKeys.cs 与 default.json 双向对应、manifest 字段与版本号占位符、
# csproj 的 TFM 与 Version、以及 PR 中 content/[CP]Red Panda Bazaar/ 是否被改动
dotnet run --project "tools/RedPandaBazaar.RepoTools" -- validate
dotnet test "tests/RedPandaBazaar.Tests/RedPandaBazaar.Tests.csproj"
```

`.github/workflows/ci.yml` 在 `main` / `develop` 的 push 与 PR 上运行：

- **仓库一致性校验与单元测试**（`validate`）：跑上面的 RepoTools `validate` 与 `dotnet test`（含仓库冒烟）。PR 中 `content/[CP]Red Panda Bazaar/` 改动会失败，同步上游时给 PR 打上 `cp-change-approved` 标签即可豁免。
- **编译检查**（`build`）：在 `ubuntu-latest` 上编译整个模组（0 error、0 可修复警告）。runner 上没有游戏本体，环境准备抽在复合 action `.github/actions/setup-build/action.yml`（CI 与 CD 共用）：
  1. `StardewModders/mod-reference-assemblies` 的公开 API 空壳（`actions/checkout@v5` 直接克隆到 `$GITHUB_WORKSPACE/_game`）；
  2. 克隆 `Mushymato/StardewUI` 固定提交（见 Runtime/Tooling 表）到 `_stardewui`；
  3. `-p:EnableModDeploy=false -p:EnableModZip=false` 关掉全部副作用，`-p:GamePath` 与 `-p:StardewUICorePath` 显式指向上述目录。

  **SDK 版本**：StardewUI 用到 C# 13/14 语法（`ref struct` 接口等），net6.0 SDK 自带的 C# 10 编译不过，因此用 `actions/setup-dotnet` 装 `10.0.x`，而不是 Pathoschild 的 `add-build-environment`（它会把 SDK 固定回 6.0）。
- **RC 发布**（`release`，CD）：仅在 **push 到 `develop`** 且前两个 job 都通过后运行。构建出 `x.y.z-rc.<日期>.<run>` 版本的 zip 并刷新**同一条** GitHub Release（tag 固定 `rc`）——release 与 tag 永不删除，只原地更新标题、说明与产物，保证玩家手里的下载链接不中断。

  **同一条 release 的实现要点**：`gh release delete` 会造成下载链接的中间态中断，因此用 `gh release edit` + `gh release upload` 原地刷新（先传新资产再清旧资产，顺序不能反），并显式传 `--latest`。CD 需要 `contents: write`（`ci.yml` 顶层仍是 `contents: read`，只有 `release` job 单独提权）。

  **资产名不可用本地 zip 名比对**：GitHub 会重命名含空格等特殊字符的资产文件名（REST「Upload a release asset」的 Notes）。因此清理旧资产时不能拿 `"Red Panda Bazaar Code ${VERSION}.zip"` 逐字比对来认领本次产物——匹配不上就会把刚上传的资产当旧资产删掉，release 归零而 CI 仍报成功（历史上曾如此，下载链全部 404）。改为按 `createdAt` 认领（不早于本次上传起点，留 120s 时钟余量），并在清理后断言资产数为 1。改这段脚本时不要退回按文件名比对。

  **不要标记 prerelease**：GitHub 规定 prerelease 永远不能成为 latest。一旦打上该标记，`/releases/latest` 会返回 404。因此创建与刷新都显式传 `--latest`。

其余验证方式是**编译 + 游戏内人工确认**：

```bash
# 能做到的自动验证：编译通过（0 error），零副作用
dotnet build "src/RedPandaBazaar/RedPandaBazaar.csproj" -p:EnableModDeploy=false -p:EnableModZip=false
```

改动完成后，交付前至少要保证编译通过；涉及游戏内行为的部分，需**列出可执行的验证清单**交给开发者在游戏内确认，而不是声称已验证。

**调试设施**：通用调试功能（可视化开关、Tile Action 高亮、调试菜单、光标传送）已拆分到独立模组 **Dev Debug Kit**（`WhaleK233.DevDebugKit`，位于本仓库之外 `MySDVMods/Dev Debug Kit/`），键位在其 GMCM 中配置。本模组仅保留专属调试 UI（摊位菜单的 Action ID 叠层），经 `Integrations.Installed.DevDebugKitApi` 读取其开关状态（未安装时自动关闭），自己不再有任何调试配置项。

**已知的仓库卫生问题**（改动时顺带注意，不要扩散）：

- `src/RedPandaBazaar/out/net6.0/` 的历史 zip 随构建持续堆积（已 gitignore），定期清理。
