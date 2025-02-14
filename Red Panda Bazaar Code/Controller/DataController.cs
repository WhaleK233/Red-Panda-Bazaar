using Red_Panda_Bazaar_Code.Data;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Red_Panda_Bazaar_Code.Controller;

public static class DataController
{
    public static void Init()
    {
        Tools.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Tools.Helper.Events.GameLoop.Saving += OnSaving;

        Tools.Log("Data Initialized.");
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        RPBData.PrizeTicketReward =
            int.Parse(Tools.Helper.Data.ReadSaveData<string>(RPBData.Keys.PrizeTicketRewardKey) ?? "0");
        Tools.Log($"Loaded Prize Ticket Reward");
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        if (!Context.IsMainPlayer) return;

        Tools.Helper.Data.WriteSaveData(RPBData.Keys.PrizeTicketRewardKey, RPBData.PrizeTicketReward.ToString());
        Tools.Log($"Saved Prize Ticket Reward");
    }
}