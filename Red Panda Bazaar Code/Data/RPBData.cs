using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Data;

public static class RPBData
{
    public static int PrizeRandomIntPerWeek { get; set; }

    public static int PrizeTicketReward { get; set; }

    public static void Init()
    {
        Tools.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.Saving += OnSaving;
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        Tools.Helper.Data.WriteSaveData(Keys.PrizeRandomIntPerWeekKey, PrizeRandomIntPerWeek.ToString());
        Tools.Log($"Saving Prize Random Number");

        Tools.Helper.Data.WriteSaveData(Keys.PrizeTicketRewardKey, PrizeTicketReward.ToString());
        Tools.Log($"Saving Prize Ticket Reward");
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PrizeRandomIntPerWeek = int.Parse(Tools.Helper.Data.ReadSaveData<string>(Keys.PrizeRandomIntPerWeekKey) ??
                                          $"{Game1.random.Next()}");
        Tools.Log($"Loaded Prize Random Number");

        PrizeRandomIntPerWeek = int.Parse(Tools.Helper.Data.ReadSaveData<string>(Keys.PrizeTicketRewardKey) ?? "0");
        Tools.Log($"Loaded Prize Ticket Reward");
    }

    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Game1.dayOfMonth % 7 == 1)
        {
            PrizeRandomIntPerWeek = Game1.random.Next();
        }

        Tools.Log($"Today's Prize Random Number is {PrizeRandomIntPerWeek}");
    }

    public static int PrizeRandomIntIncrement()
    {
        return ++PrizeRandomIntPerWeek;
    }

    public static int PrizeTicketRewardIncrement()
    {
        return ++PrizeTicketReward;
    }

    public static int PrizeTicketRewardDecrement()
    {
        return --PrizeTicketReward;
    }

    private static class Keys
    {
        public const string PrizeRandomIntPerWeekKey = "PrizeRandomIntPerWeek";
        public const string PrizeTicketRewardKey = "PrizeTicketReward";
    }
}