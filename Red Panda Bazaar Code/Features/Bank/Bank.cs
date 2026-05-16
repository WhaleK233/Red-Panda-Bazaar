using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class Bank
{
    private const string SaveKey = StatsKeys.BankSave;
    private const string BankLocation = "Custom_RedPandaBazaarBank1";

    private static BankSaveData Data { get; set; } = new();

    /// <summary>客机不读写存档，靠主机广播保持同步。</summary>
    private static BankSaveData? _clientCache;

    public static void Init()
    {
        Tools.Log("Bank Initializing.");
        GameLocation.RegisterTileAction("RedPandaBazaar_BankMenu", OnTileAction);
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.Saving += OnSaving;
        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        Tools.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        Tools.Log("Bank Initialized.");
    }

    private static bool OnTileAction(GameLocation location, string[] args, Farmer who, Point tile)
    {
        ShowBankServiceDialogue(location);
        return false;
    }

    private static void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Tools.IsValidButtonAction(e)) return;
        if (Game1.currentLocation is not { Name: BankLocation }) return;

        var tile = e.Cursor.GrabTile;
        var action = Game1.currentLocation.doesTileHaveProperty(
            (int)tile.X, (int)tile.Y, "Action", "Buildings");
        if (action != "RedPandaBazaar_BankMenu") return;

        ShowBankServiceDialogue(Game1.currentLocation);
        Tools.Helper.Input.Suppress(e.Button);
    }

    /// <summary>弹出业务选择对话框。</summary>
    private static void ShowBankServiceDialogue(GameLocation location)
    {
        location.createQuestionDialogue(
            Tools.GetI18n(I18nKeys.Bank_ServiceQuestion).ToString(),
            new Response[]
            {
                new("Checking", Tools.GetI18n(I18nKeys.Bank_ServiceChecking).ToString()),
                new("Fixed", Tools.GetI18n(I18nKeys.Bank_ServiceFixed).ToString()),
                new("Loan", Tools.GetI18n(I18nKeys.Bank_ServiceLoan).ToString()),
                new("Tax", Tools.GetI18n(I18nKeys.Bank_ServiceTax).ToString()),
                new("Cancel", Tools.GetI18n(I18nKeys.Bank_ServiceCancel).ToString()),
            },
            OnBankDialogResponse);
    }

    private static void OnBankDialogResponse(Farmer who, string whichAnswer)
    {
        switch (whichAnswer)
        {
            case "Checking":
                Game1.activeClickableMenu = new BankCheckingMenu();
                break;
            case "Fixed":
                Game1.activeClickableMenu = new BankFixedMenu();
                break;
            case "Loan":
                Game1.activeClickableMenu = new BankLoanMenu();
                break;
            case "Tax":
                Game1.activeClickableMenu = new BankTaxMenu();
                break;
        }
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            Data = Tools.Helper.Data.ReadSaveData<BankSaveData>(SaveKey) ?? new BankSaveData();
        }
        else
        {
            Data = new BankSaveData();
        }
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer) WriteSaveData();
    }

    public static void WriteSaveData()
    {
        Tools.Helper.Data.WriteSaveData(SaveKey, Data);
    }

    /// <summary>每日利息结算及到期提醒（仅主机执行）。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            SettleDailyInterest();
            CheckMaturedDeposits();
            BroadcastSyncData();
        }
    }

    /// <summary>检查是否有到期的定期存款未领取。</summary>
    private static void CheckMaturedDeposits()
    {
        var now = (int)Game1.stats.DaysPlayed;
        var matured = Data.FixedDeposits.Any(d => !d.Withdrawn && now - d.StartDay >= d.TermDays);
        if (matured)
            Game1.chatBox?.addMessage(
                Tools.GetI18n(I18nKeys.Bank_FixedMatureReminder).ToString(), Color.Green);
    }

    /// <summary>按天逐日复利结算活期利息。</summary>
    private static void SettleDailyInterest()
    {
        if (Data.LastInterestDay >= Game1.stats.DaysPlayed) return;

        var daysElapsed = (int)Game1.stats.DaysPlayed - Data.LastInterestDay;
        if (daysElapsed <= 0) return;

        for (var i = 0; i < daysElapsed; i++)
        {
            var rate = BankCalculator.GetDailyCheckingRate();
            Data.CheckingBalance += (long)(Data.CheckingBalance * rate);

            foreach (var loan in Data.Loans.Where(l => !l.Repaid))
            {
                var loanRate = BankCalculator.LoanDailyRate[loan.PlanType];
                loan.InterestAccrued += (long)(loan.Principal * loanRate);
            }
        }

        Data.LastInterestDay = (int)Game1.stats.DaysPlayed;
    }

    // ====== 读取接口 ======

    public static long GetCheckingBalance() => Context.IsMainPlayer ? Data.CheckingBalance : (_clientCache?.CheckingBalance ?? 0);
    public static int GetLastInterestDay() => Context.IsMainPlayer ? Data.LastInterestDay : (_clientCache?.LastInterestDay ?? 0);

    public static List<FixedDeposit> GetFixedDeposits()
    {
        return Context.IsMainPlayer ? Data.FixedDeposits.ToList() : (_clientCache?.FixedDeposits.ToList() ?? new());
    }

    public static List<LoanAccount> GetLoans()
    {
        return Context.IsMainPlayer ? Data.Loans.ToList() : (_clientCache?.Loans.ToList() ?? new());
    }

    // ====== 操作接口 ======
    // 主机直接操作存档数据后广播同步；客机先改本地玩家金币、再将请求发给主机验证。

    public static void Deposit(int amount)
    {
        if (amount <= 0) return;
        if (Context.IsMainPlayer)
        {
            if (Game1.player.Money < amount) return;
            Game1.player.Money -= amount;
            Data.CheckingBalance += amount;
            BroadcastSyncData();
        }
        else
        {
            if (Game1.player.Money < amount) return;
            Game1.player.Money -= amount;
            SendActionRequest("deposit", amount, 0);
        }
    }

    public static void Withdraw(int amount)
    {
        if (amount <= 0) return;
        if (Context.IsMainPlayer)
        {
            if (Data.CheckingBalance < amount) return;
            Data.CheckingBalance -= amount;
            Game1.player.Money += amount;
            BroadcastSyncData();
        }
        else
        {
            SendActionRequest("withdraw", amount, 0);
        }
    }

    public static void CreateFixedDeposit(int amount, int termDays)
    {
        if (amount <= 0 || !BankCalculator.FixedTermOptions.Contains(termDays)) return;
        if (Context.IsMainPlayer)
        {
            if (Game1.player.Money < amount) return;
            Game1.player.Money -= amount;
            Data.FixedDeposits.Add(new FixedDeposit
            {
                Amount = amount,
                TermDays = termDays,
                StartDay = (int)Game1.stats.DaysPlayed,
                Withdrawn = false
            });
            BroadcastSyncData();
        }
        else
        {
            if (Game1.player.Money < amount) return;
            Game1.player.Money -= amount;
            SendActionRequest("newFixed", amount, termDays);
        }
    }

    public static void RedeemFixedDeposit(int depositIndex)
    {
        if (Context.IsMainPlayer)
        {
            ExecuteRedeemFixedDeposit(depositIndex, null);
            BroadcastSyncData();
        }
        else
        {
            SendActionRequest("redeemFixed", 0, depositIndex);
        }
    }

    public static void EarlyWithdrawFixedDeposit(int depositIndex)
    {
        if (Context.IsMainPlayer)
        {
            ExecuteEarlyWithdrawFixedDeposit(depositIndex, null);
            BroadcastSyncData();
        }
        else
        {
            SendActionRequest("earlyWithdraw", 0, depositIndex);
        }
    }

    public static void ApplyLoan(int planType)
    {
        if (planType < 0 || planType > 2) return;
        if (Context.IsMainPlayer)
        {
            ExecuteApplyLoan(planType, null);
            BroadcastSyncData();
        }
        else
        {
            SendActionRequest("applyLoan", 0, planType);
        }
    }

    public static void RepayLoan(int loanIndex)
    {
        if (Context.IsMainPlayer)
        {
            ExecuteRepayLoan(loanIndex, null);
            BroadcastSyncData();
        }
        else
        {
            SendActionRequest("repayLoan", 0, loanIndex);
        }
    }

    // ====== 主机执行方法 ======
    // playerId 为请求方玩家，主机用它操作对应玩家的金币（NetInt 自动同步到客机）。
    // null = 主机自己在操作（单机或主机玩家本人）。

    private static Farmer? GetFarmer(long? playerId)
    {
        if (!playerId.HasValue) return Game1.player;
        return Game1.GetPlayer(playerId.Value) ?? Game1.MasterPlayer;
    }

    private static void ExecuteDeposit(int amount, long? playerId)
    {
        Data.CheckingBalance += amount;
    }

    private static void ExecuteWithdraw(int amount, long? playerId)
    {
        if (Data.CheckingBalance < amount) return;
        Data.CheckingBalance -= amount;
        var farmer = GetFarmer(playerId);
        if (farmer != null) farmer.Money += amount;
    }

    private static void ExecuteCreateFixedDeposit(int amount, int termDays, long? playerId)
    {
        var farmer = GetFarmer(playerId);
        if (farmer == null || farmer.Money < amount) return;
        farmer.Money -= amount;
        Data.FixedDeposits.Add(new FixedDeposit
        {
            Amount = amount,
            TermDays = termDays,
            StartDay = (int)Game1.stats.DaysPlayed,
            Withdrawn = false
        });
    }

    private static void ExecuteRedeemFixedDeposit(int depositIndex, long? playerId)
    {
        if (depositIndex < 0 || depositIndex >= Data.FixedDeposits.Count) return;
        var deposit = Data.FixedDeposits[depositIndex];
        if (deposit.Withdrawn) return;

        var elapsed = (int)Game1.stats.DaysPlayed - deposit.StartDay;
        if (elapsed < deposit.TermDays) return;

        var rate = BankCalculator.GetFixedTermRate(deposit.TermDays);
        var interest = (long)(deposit.Amount * rate);
        var farmer = GetFarmer(playerId);
        if (farmer != null) farmer.Money += (int)(deposit.Amount + interest);
        deposit.Withdrawn = true;
    }

    private static void ExecuteEarlyWithdrawFixedDeposit(int depositIndex, long? playerId)
    {
        if (depositIndex < 0 || depositIndex >= Data.FixedDeposits.Count) return;
        var deposit = Data.FixedDeposits[depositIndex];
        if (deposit.Withdrawn) return;

        var rate = BankCalculator.GetFixedTermRate(deposit.TermDays);
        var interest = (long)(deposit.Amount * rate * 0.5);
        var farmer = GetFarmer(playerId);
        if (farmer != null) farmer.Money += (int)(deposit.Amount + interest);
        deposit.Withdrawn = true;
    }

    private static void ExecuteApplyLoan(int planType, long? playerId)
    {
        var farmer = GetFarmer(playerId);
        if (farmer == null) return;

        // 每种方案同时只能有一笔未还贷款
        if (Data.Loans.Any(l => !l.Repaid && l.PlanType == planType)) return;

        var remaining = BankCalculator.GetRemainingCredit(Data.Loans);
        var amount = BankCalculator.GetAvailableLoanAmount(planType, remaining, Data.Loans);
        if (amount <= 0) return;

        Data.Loans.Add(new LoanAccount
        {
            PlanType = planType,
            Principal = amount,
            StartDay = (int)Game1.stats.DaysPlayed,
            InterestAccrued = 0,
            Repaid = false
        });
        farmer.Money += (int)amount;
    }

    private static void ExecuteRepayLoan(int loanIndex, long? playerId)
    {
        if (loanIndex < 0 || loanIndex >= Data.Loans.Count) return;
        var loan = Data.Loans[loanIndex];
        if (loan.Repaid) return;

        var total = (int)(loan.Principal + loan.InterestAccrued);
        var farmer = GetFarmer(playerId);
        if (farmer == null || farmer.Money < total) return;

        farmer.Money -= total;
        loan.Repaid = true;
    }


    // ====== 多人同步 ======

    private static void SendActionRequest(string action, int amount, int param)
    {
        var data = new BankActionRequestData
        {
            Action = action,
            Amount = amount,
            Param = param
        };
        Tools.Helper.Multiplayer.SendMessage(
            data, MPMessageType.Bank_ActionRequest,
            modIDs: new[] { Tools.ModManifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private static void BroadcastSyncData()
    {
        var syncData = new BankSyncData
        {
            CheckingBalance = Data.CheckingBalance,
            FixedDeposits = Data.FixedDeposits,
            Loans = Data.Loans,
            LastInterestDay = Data.LastInterestDay
        };
        Tools.Helper.Multiplayer.SendMessage(
            syncData, MPMessageType.Bank_SyncData,
            modIDs: new[] { Tools.ModManifest.UniqueID });
    }

    private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != Tools.ModManifest.UniqueID) return;

        switch (e.Type)
        {
            case MPMessageType.Bank_SyncData:
                if (Context.IsMainPlayer) break;
                var syncData = e.ReadAs<BankSyncData>();
                if (syncData != null)
                {
                    _clientCache = new BankSaveData
                    {
                        CheckingBalance = syncData.CheckingBalance,
                        FixedDeposits = syncData.FixedDeposits,
                        Loans = syncData.Loans,
                        LastInterestDay = syncData.LastInterestDay
                    };
                }
                break;

            case MPMessageType.Bank_ActionRequest:
                if (!Context.IsMainPlayer) break;
                var request = e.ReadAs<BankActionRequestData>();
                if (request == null) break;

                SettleDailyInterest();
                long? pid = e.FromPlayerID;

                switch (request.Action)
                {
                    case "deposit":
                        ExecuteDeposit(request.Amount, pid);
                        break;
                    case "withdraw":
                        ExecuteWithdraw(request.Amount, pid);
                        break;
                    case "newFixed":
                        ExecuteCreateFixedDeposit(request.Amount, request.Param, pid);
                        break;
                    case "redeemFixed":
                        ExecuteRedeemFixedDeposit(request.Param, pid);
                        break;
                    case "earlyWithdraw":
                        ExecuteEarlyWithdrawFixedDeposit(request.Param, pid);
                        break;
                    case "applyLoan":
                        ExecuteApplyLoan(request.Param, pid);
                        break;
                    case "repayLoan":
                        ExecuteRepayLoan(request.Param, pid);
                        break;
                }

                BroadcastSyncData();
                break;
        }
    }
}

/// <summary>客机→主机的操作请求数据。</summary>
public class BankActionRequestData
{
    public string Action { get; set; } = "";
    public int Amount { get; set; }
    public int Param { get; set; }
}
