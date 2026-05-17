using Red_Panda_Bazaar_Code.Config;
using Red_Panda_Bazaar_Code.Constant;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

public static class Tools
{
    public static IModHelper Helper { get; set; }
    public static ModConfig ModConfig { get; set; }
    public static IMonitor Monitor { get; set; }
    public static ITranslationHelper I18n { get; set; }

    public static IManifest ModManifest { get; set; }

    public static void Init(IModHelper helper, ModConfig modConfig, IMonitor monitor, IManifest manifest) {
        Helper = helper;
        ModConfig = modConfig;
        Monitor = monitor;
        ModManifest = manifest;
        I18n = Helper.Translation;
    }

    public static Translation GetI18n(string key) => I18n.Get(key);

    public static void Log(string message, LogLevel level = LogLevel.Trace) => Monitor.Log(message, level);

    public static void LogInfo(string message) => Log(message, LogLevel.Info);

    public static void LogPatch(string ob, string methodName, string patchType) => Log(
        $"Applying Harmony patch \"{ob}\": {patchType} SDV method \"{methodName}\".");

    public static void LogPatchErr(string ob, Exception e) =>
        LogOnce($"Harmony patch \"{ob}\" has encountered an error. Full error message: \n{e}", LogLevel.Error);

    private static void LogOnce(string message, LogLevel level = LogLevel.Trace) => Monitor.LogOnce(message, level);

    public static bool TryCharge(int cost) {
        if (Game1.player.Money >= cost) {
            Game1.player.Money -= cost;
            return true;
        }

        Game1.drawObjectDialogue(GetI18n(I18nKeys.Dialogue_MoneyNotEnough));
        return false;
    }

    /// <summary>按指定玩家扣款，支持静默失败（不弹对话）。</summary>
    public static bool TryCharge(Farmer? farmer, int cost, bool showDialogue = true) {
        if (farmer == null || farmer.Money < cost) {
            if (showDialogue)
                Game1.drawObjectDialogue(GetI18n(I18nKeys.Dialogue_MoneyNotEnough));
            return false;
        }

        farmer.Money -= cost;
        return true;
    }

    public static bool IsGoodWeather() {
        return !Game1.isRaining && !Game1.isLightning && !Game1.isSnowing;
    }

    public static bool IsDayTime(GameLocation loc) {
        return Game1.timeOfDay < Game1.getStartingToGetDarkTime(loc);
    }

    public static bool IsDuskTime(GameLocation loc) {
        return !IsDayTime(loc) && !IsNightTime(loc);
    }

    public static bool IsNightTime(GameLocation loc) {
        return Game1.timeOfDay > Game1.getTrulyDarkTime(loc);
    }

    public static bool IsValidButtonAction(ButtonPressedEventArgs e) {
        if (!Context.IsWorldReady || Game1.player.hasMenuOpen.Value) return false;

        if (Constants.TargetPlatform == GamePlatform.Android) {
            if (e.Button != SButton.MouseLeft)
                return false;
        }
        else if (!e.Button.IsActionButton()) {
            return false;
        }

        return true;
    }

    /// <summary>发送 SMAPI 多人消息：客机发往主机，主机广播到所有客机。</summary>
    public static void SendToHostOrBroadcast<T>(T data, string messageType) {
        if (Context.IsMainPlayer) {
            Helper.Multiplayer.SendMessage(data, messageType, modIDs: new[] { ModManifest.UniqueID });
        } else {
            Helper.Multiplayer.SendMessage(data, messageType, modIDs: new[] { ModManifest.UniqueID }, playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }
}