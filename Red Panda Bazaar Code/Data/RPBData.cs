using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code.Data;

public static class RPBData
{
    public static int PrizeTicketReward { get; private set; }

    public static void Init()
    {
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.Saving += OnSaving;
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PrizeTicketReward = int.Parse(Tools.Helper.Data.ReadSaveData<string>(Keys.PrizeTicketRewardKey) ?? "0");
        Tools.Log($"Loaded Prize Ticket Reward");
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        Tools.Helper.Data.WriteSaveData(Keys.PrizeTicketRewardKey, PrizeTicketReward.ToString());
        Tools.Log($"Saved Prize Ticket Reward");
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
        public const string PrizeTicketRewardKey = "PrizeTicketReward";
    }
}