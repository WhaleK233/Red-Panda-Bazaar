using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

/// <summary>本 mod 的通用工具函数：日志、翻译、扣款、多人消息和通用判断。</summary>
public static class Tools
{
    private static IModHelper? _helper;
    private static string? _modId;
    private static IMonitor? _monitor;

    /// <summary>钱不够时的提示文本，由本 mod 在启动时注入。</summary>
    public static Func<string>? InsufficientFundsMessage { get; set; }

    /// <summary>Mod 启动时调用一次，注入运行时上下文。</summary>
    public static void Init(
        IModHelper helper,
        IMonitor monitor,
        string modId,
        Func<string>? insufficientFundsMessage = null)
    {
        _helper = helper;
        _modId = modId;
        _monitor = monitor;
        InsufficientFundsMessage = insufficientFundsMessage;
    }

    public static Translation GetI18n(string key) => _helper!.Translation.Get(key);

    public static Translation GetI18n(string key, object tokens) => _helper!.Translation.Get(key, tokens);

    public static void Log(string message, LogLevel level = LogLevel.Trace) => _monitor?.Log(message, level);

    public static void LogInfo(string message) => Log(message, LogLevel.Info);

    public static void LogWarn(string message) => Log(message, LogLevel.Warn);

    public static void LogErr(string message) => Log(message, LogLevel.Error);

    public static void LogPatch(string ob, string methodName, string patchType) => Log(
        $"Applying Harmony patch \"{ob}\": {patchType} SDV method \"{methodName}\".");

    /// <summary>记录补丁异常，避免高频补丁重复刷屏。</summary>
    public static void LogPatchErr(string ob, Exception e) =>
        _monitor?.LogOnce(
            $"Harmony patch \"{ob}\" has encountered an error. Full error message: \n{e}",
            LogLevel.Error);

    public static bool TryCharge(int cost)
    {
        if (Game1.player.Money >= cost)
        {
            Game1.player.Money -= cost;
            return true;
        }

        if (InsufficientFundsMessage != null)
            Game1.drawObjectDialogue(InsufficientFundsMessage());
        return false;
    }

    public static bool TryCharge(Farmer? farmer, int cost, bool showDialogue = true)
    {
        if (farmer == null || farmer.Money < cost)
        {
            if (showDialogue && InsufficientFundsMessage != null)
                Game1.drawObjectDialogue(InsufficientFundsMessage());
            return false;
        }

        farmer.Money -= cost;
        return true;
    }

    public static bool IsGoodWeather() => !Game1.isRaining && !Game1.isLightning && !Game1.isSnowing;

    public static bool IsValidButtonAction(ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.player.hasMenuOpen.Value)
            return false;

        if (Constants.TargetPlatform == GamePlatform.Android)
            return e.Button == SButton.MouseLeft;

        return e.Button.IsActionButton();
    }

    public static void SendToHostOrBroadcast<T>(T data, string messageType)
    {
        if (Context.IsMainPlayer)
        {
            _helper!.Multiplayer.SendMessage(data, messageType, modIDs: new[] { _modId! });
        }
        else
        {
            _helper!.Multiplayer.SendMessage(
                data,
                messageType,
                modIDs: new[] { _modId! },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }
}
