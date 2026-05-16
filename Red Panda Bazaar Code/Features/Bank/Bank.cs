using Microsoft.Xna.Framework;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Features.Bank;

public static class Bank
{
    private const string SaveKey = "WhaleK233.RedPandaBazaar.Bank";
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
        Game1.activeClickableMenu = new BankMenu();
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

        Game1.activeClickableMenu = new BankMenu();
        Tools.Helper.Input.Suppress(e.Button);
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

    /// <summary>每日利息结算（仅主机执行）。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer)
        {
            SettleDailyInterest();
            BroadcastSyncData();
        }
    }

    /// <summary>按天逐日结算活期利息。</summary>
    private static void SettleDailyInterest()
    {
        if (Data.LastInterestDay >= Game1.stats.DaysPlayed) return;

        var daysElapsed = (int)Game1.stats.DaysPlayed - Data.LastInterestDay;
        if (daysElapsed <= 0) return;

        for (var i = 0; i < daysElapsed; i++)
        {
            var rate = BankCalculator.GetDailyCheckingRate();
            Data.InterestEarned += (int)(Data.CheckingBalance * rate);
        }

        Data.LastInterestDay = (int)Game1.stats.DaysPlayed;
    }

    // ====== 读取接口 ======

    public static int GetCheckingBalance() => Context.IsMainPlayer ? Data.CheckingBalance : (_clientCache?.CheckingBalance ?? 0);
    public static int GetInterestEarned() => Context.IsMainPlayer ? Data.InterestEarned : (_clientCache?.InterestEarned ?? 0);
    public static int GetLastInterestDay() => Context.IsMainPlayer ? Data.LastInterestDay : (_clientCache?.LastInterestDay ?? 0);

    public static List<FixedDeposit> GetFixedDeposits()
    {
        return Context.IsMainPlayer ? Data.FixedDeposits.ToList() : (_clientCache?.FixedDeposits.ToList() ?? new());
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

    public static void ClaimInterest()
    {
        if (Context.IsMainPlayer)
        {
            if (Data.InterestEarned <= 0) return;
            Game1.player.Money += Data.InterestEarned;
            Data.InterestEarned = 0;
            BroadcastSyncData();
        }
        else
        {
            SendActionRequest("claim", 0, 0);
        }
    }

    public static void CreateFixedDeposit(int amount, int termDays)
    {
        if (amount <= 0 || !BankCalculator.FixedTermOptions.Contains(termDays)) return;
        if (Context.IsMainPlayer)
        {
            if (Data.CheckingBalance < amount) return;
            Data.CheckingBalance -= amount;
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

    private static void ExecuteClaimInterest(long? playerId)
    {
        if (Data.InterestEarned <= 0) return;
        var farmer = GetFarmer(playerId);
        if (farmer != null) farmer.Money += Data.InterestEarned;
        Data.InterestEarned = 0;
    }

    private static void ExecuteCreateFixedDeposit(int amount, int termDays, long? playerId)
    {
        if (Data.CheckingBalance < amount) return;
        Data.CheckingBalance -= amount;
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
        var interest = (int)(deposit.Amount * rate);
        Data.CheckingBalance += deposit.Amount + interest;
        deposit.Withdrawn = true;
    }

    private static void ExecuteEarlyWithdrawFixedDeposit(int depositIndex, long? playerId)
    {
        if (depositIndex < 0 || depositIndex >= Data.FixedDeposits.Count) return;
        var deposit = Data.FixedDeposits[depositIndex];
        if (deposit.Withdrawn) return;

        var rate = BankCalculator.GetFixedTermRate(deposit.TermDays);
        var interest = (int)(deposit.Amount * rate * 0.5);
        Data.CheckingBalance += deposit.Amount + interest;
        deposit.Withdrawn = true;
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
            InterestEarned = Data.InterestEarned,
            FixedDeposits = Data.FixedDeposits,
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
                        InterestEarned = syncData.InterestEarned,
                        FixedDeposits = syncData.FixedDeposits,
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
                    case "claim":
                        ExecuteClaimInterest(pid);
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
