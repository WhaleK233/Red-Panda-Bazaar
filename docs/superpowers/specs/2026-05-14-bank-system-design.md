# 银行系统设计文档

**日期：** 2026-05-14  
**模组：** Red Panda Bazaar  
**建筑：** Custom_RedPandaBazaarBank1（已有 CP 地图，枫林桥 LockedDoorWarp 进入）

## 1. 功能总览

为小熊猫集市银行建筑实现完整的金融系统，包含四个核心功能：活期存款、定期存款、贷款、税收记录。

## 2. 数据模型 (`Bank/BankData.cs`)

```csharp
// 玩家存档数据
class BankSaveData {
    // 活期
    int CheckingBalance;                  // 活期余额
    int InterestEarned;                   // 待领取利息（每日自动结算，手动领取）
    
    // 定期
    List<FixedDeposit> FixedDeposits;
    
    // 贷款
    List<LoanAccount> Loans;
    
    // 记录
    int LastInterestDay;                  // 上次结算利息时的 DaysPlayed
}

class FixedDeposit {
    int Amount;                           // 本金
    int TermDays;                         // 期限 7/28/112
    int StartDay;                         // 存入时的 DaysPlayed
    bool Withdrawn;                       // 是否已取出
}

class LoanAccount {
    int Principal;                        // 贷款本金
    int StartDay;                         // 贷款发放时的 DaysPlayed
    int InterestAccrued;                  // 已产生利息
    bool Repaid;                          // 是否已还清
}
```

## 3. 利率计算器 (`Bank/BankCalculator.cs`)

### 活期日利率

```
rate = 0.0005 × (1 + DailyLuck × 2) × (1 + sin(π × DayOfMonth / 28) × 0.3)
```

- 基准利率 0.05%/天
- 受每日运气影响，波动 ±20%
- 受天数影响，月初月末低、月中高，波动 ±30%

### 定期利率（到期一次性）

```
7天期    = 活期日利率 × 7   × 1.5
28天期   = 活期日利率 × 28  × 2.0
112天期  = 活期日利率 × 112 × 2.5
```

提前支取：利息减半支付。

### 贷款利率

```
日利率 = 0.001 × (1 + min(逾期天数 / 112, 1.0))
```

- 贷款第 1 天 0.1%/天，第 112 天起封顶 0.2%/天
- 利息独立累计，不滚入本金

## 4. 核心模块 (`Bank/Bank.cs`)

### 初始化

遵循项目模式，在 `FeatureInit()` 中调用 `Bank.Init()`：

```csharp
public static void Init() {
    GameLocation.RegisterTileAction("RedPandaBazaar_Bank", OnTileAction);
    
    Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    Tools.Helper.Events.GameLoop.Saving += OnSaving;
    Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
    Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
    Tools.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
}
```

### 每日结算（主机执行）

1. 校验 `LastInterestDay < DaysPlayed` 防止重复结算
2. 活期：`InterestEarned += (int)(CheckingBalance × 活期日利率)`
3. 贷款：遍历 `Loans`，每笔 `InterestAccrued += Principal × 当日贷款利率`
4. 更新 `LastInterestDay = DaysPlayed`
5. 主机广播同步数据给所有客机

### 多人同步

参考 PlayerStall 的同步模式：
- 客机 `DayStarted` 时向主机请求完整数据
- 主机作为权威端执行所有金融操作（存/取/贷/还），广播结果
- 客机应用广播数据，保持 UI 同步

## 5. 菜单界面 (`Bank/BankMenu.cs`)

继承 `IClickableMenu`，顶部四个选项卡：

| 选项卡 | 功能 |
|--------|------|
| 活期 | 显示余额、待领利息（含[领取]按钮）、今日利率；[存款][取款]操作 |
| 定期 | 定期列表（每笔显示金额/期限/状态），[新开定期][领取本息][提前支取] |
| 贷款 | 当前贷款详情、[申请新贷款][还款] |
| 税收 | 显示累计缴税额（直接从 `PlayerStall.TotalTax` 读取） |

### 活期界面

- [存款] → 数字输入 → 从玩家金币扣减 → 增加 `CheckingBalance`
- [取款] → 数字输入 → 从 `CheckingBalance` 扣减 → 增加玩家金币
- [领取] → `InterestEarned` 全部转入玩家金币 → 归零

### 定期界面

- [新开定期] → 选择档位(7/28/112天) → 输入金额 → 从活期扣款 → 创建 `FixedDeposit`
- [领取本息] → 到期后本金+(本金×定期利率) 转入活期
- [提前支取] → 本金+(本金×定期利率/2) 转入活期

### 贷款界面

- [申请新贷款] → 输入金额 → 增加玩家金币 → 创建 `LoanAccount`
- [还款] → 从玩家扣 `Principal + InterestAccrued` → 标记已还清

## 6. 修改清单

### 新增文件
| 路径 | 用途 |
|------|------|
| `Features/Bank/Bank.cs` | 核心模块：初始化、事件、结算、多人同步 |
| `Features/Bank/BankData.cs` | 存档数据结构 |
| `Features/Bank/BankCalculator.cs` | 利率计算 |
| `Features/Bank/BankMenu.cs` | 菜单界面 |

### 修改文件
| 路径 | 修改内容 |
|------|----------|
| `ModEntry.cs` | 添加 `Bank.Init()` 调用 |
| `Constant/I18nKeys.cs` | 添加银行相关翻译键 |
| `i18n/default.json` | 添加银行文本翻译 |
| `Config/ModConfig.cs` | （可选）添加银行相关配置项 |

## 7. 后续可扩展

- 税收奖励系统（累计缴税兑换物品/buff）
- 贷款额度上限（基于信用/存款/游戏进度）
- 转账功能（多人模式玩家间转账）
