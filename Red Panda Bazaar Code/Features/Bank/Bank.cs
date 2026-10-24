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
        ModInfo.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        ModInfo.Helper.Events.GameLoop.Saving += OnSaving;
        ModInfo.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        ModInfo.Helper.Events.Input.ButtonPressed += OnButtonPressed;
        ModInfo.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
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
        ModInfo.Helper.Input.Suppress(e.Button);
    }

    /// <summary>弹出业务选择对话框。</summary>
    private static void ShowBankServiceDialogue(GameLocation location)
    {
        location.createQuestionDialogue(
            Tools.GetI18n(I18nKeys.Bank_ServiceQuestion).ToString(),
            new Response[]
            {
                new("Investment", Tools.GetI18n(I18nKeys.Bank_ServiceInvestment).ToString()),
                new("Cancel", Tools.GetI18n(I18nKeys.Bank_ServiceCancel).ToString()),
            },
            OnBankDialogResponse);
    }

    private static void OnBankDialogResponse(Farmer who, string whichAnswer)
    {
        switch (whichAnswer)
        {
            case "Investment":
                Game1.activeClickableMenu = new BankInvestmentMenu();
                break;
        }
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Data = new BankSaveData();
        _clientCache = null;

        if (Context.IsOnHostComputer)
        {
            Data = ModInfo.Helper.Data.ReadSaveData<BankSaveData>(SaveKey) ?? new BankSaveData();
        }
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer) WriteSaveData();
    }

    public static void WriteSaveData()
    {
        ModInfo.Helper.Data.WriteSaveData(SaveKey, Data);
    }

    /// <summary>每日贷款利息结算（仅主机执行）。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            SettleDailyInterest();
            BroadcastSyncData();
        }
    }

    /// <summary>按天逐日结算未还贷款利息。</summary>
    private static void SettleDailyInterest()
    {
        if (Data.LastInterestDay >= Game1.stats.DaysPlayed) return;

        var daysElapsed = (int)Game1.stats.DaysPlayed - Data.LastInterestDay;
        if (daysElapsed <= 0) return;

        for (var i = 0; i < daysElapsed; i++)
        {
            foreach (var loan in Data.Loans.Where(l => !l.Repaid))
            {
                var loanRate = BankCalculator.LoanDailyRate[loan.PlanType];
                loan.InterestAccrued += (long)(loan.Principal * loanRate);
            }
        }

        Data.LastInterestDay = (int)Game1.stats.DaysPlayed;
    }

    // ====== 读取接口 ======

    public static List<LoanAccount> GetLoans()
    {
        return Context.IsMainPlayer ? Data.Loans.ToList() : (_clientCache?.Loans.ToList() ?? new());
    }

    // ====== 操作接口 ======
    // 主机直接操作存档数据后广播同步；客机仅发请求，由主机验证并执行金钱操作。

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
            SendActionRequest("applyLoan", planType);
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
            SendActionRequest("repayLoan", loanIndex);
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

    private static void ExecuteApplyLoan(int planType, long? playerId)
    {
        var farmer = GetFarmer(playerId);
        if (farmer == null) return;

        // 每种方案同时只能有一笔未还贷款
        if (Data.Loans.Any(l => !l.Repaid && l.PlanType == planType)) return;

        var remaining = BankCalculator.GetRemainingCredit(Data.Loans, Game1.player.totalMoneyEarned);
        var amount = BankCalculator.GetAvailableLoanAmount(planType, remaining, Data.Loans, Game1.player.totalMoneyEarned);
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

        if (!Tools.TryCharge(GetFarmer(playerId), (int)(loan.Principal + loan.InterestAccrued), false)) return;
        loan.Repaid = true;
    }

    // ====== 多人同步 ======

    private static void SendActionRequest(string action, int param)
    {
        var data = new BankActionRequestData
        {
            Action = action,
            Param = param
        };
        ModInfo.Helper.Multiplayer.SendMessage(
            data, MPMessageType.Bank_ActionRequest,
            modIDs: new[] { ModInfo.ModManifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private static void BroadcastSyncData()
    {
        var syncData = new BankSyncData
        {
            Loans = Data.Loans,
            LastInterestDay = Data.LastInterestDay
        };
        ModInfo.Helper.Multiplayer.SendMessage(
            syncData, MPMessageType.Bank_SyncData,
            modIDs: new[] { ModInfo.ModManifest.UniqueID });
    }

    private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModInfo.ModManifest.UniqueID) return;

        switch (e.Type)
        {
            case MPMessageType.Bank_SyncData:
                if (Context.IsMainPlayer) break;
                var syncData = e.ReadAs<BankSyncData>();
                if (syncData != null)
                {
                    _clientCache = new BankSaveData
                    {
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
    public int Param { get; set; }
}
