using System.Reflection;
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

    /// <summary>
    /// 应用单个 Harmony 补丁，目标方法找不到时记警告并跳过，而不是让异常中断整个启动流程。
    /// 游戏更新改名或改签名时 AccessTools.Method 会返回 null，直接传给 harmony.Patch
    /// 会抛 ArgumentNullException，导致后续补丁全部不生效且玩家只看到一条难以定位的报错。
    /// </summary>
    /// <param name="patchClass">补丁类名，仅用于日志。</param>
    /// <param name="target">AccessTools.Method 的查找结果，可能为 null。</param>
    /// <param name="description">目标方法描述，用于日志。</param>
    /// <param name="apply">拿到非 null 目标后执行的实际 patch 动作。</param>
    /// <returns>补丁是否成功应用。</returns>
    public static bool PatchSafe(
        string patchClass,
        MethodInfo? target,
        string description,
        Action<MethodInfo> apply)
    {
        if (target == null)
        {
            LogWarn($"[{patchClass}] 找不到目标方法 {description}，跳过该补丁（游戏版本可能已变更）。");
            return false;
        }

        apply(target);
        return true;
    }

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

    /// <summary>
    /// 主机侧广播：主机屏与同机分屏客机共用进程内状态，一律以广播发给远端客机
    /// （分屏客机的定向消息发不到本机主机屏——SMAPI 的 Peers 只含网络连接，
    /// sendToSelf 只认当前屏玩家，消息会被静默丢弃）；远程客机则定向发主机。
    /// </summary>
    public static void SendToHostOrBroadcast<T>(T data, string messageType)
    {
        if (Context.IsOnHostComputer)
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
