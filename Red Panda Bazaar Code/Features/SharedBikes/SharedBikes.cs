using System;
using System.Collections.Generic;
using System.IO;
using WhaleK.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Red_Panda_Bazaar_Code.Constant;
using Red_Panda_Bazaar_Code.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData;

namespace Red_Panda_Bazaar_Code.Features.SharedBikes;

/// <summary>
/// 共享单车(技术验证阶段)：每天在枫叶桥生成两匹验证坐骑，
/// 验证「动态生成原版 Horse 实体 + modData 标记」这条路线的可用性。
/// 骑乘、多人同步、跨地图跟随均复用原版机制，此处只负责实体的生成与回收。
/// </summary>
public static class SharedBikes
{
    /// <summary>枫叶桥地图内部名(CP CustomLocations 注册)。</summary>
    private const string MapleBridgeLocation = "Custom_MapleBridge";

    /// <summary>单车标记：原版 Horse 类型 + modData 附加状态标识身份，不做子类。</summary>
    public const string BikeMarkerKey = "WhaleK233.RedPandaBazaar.Bike";

    /// <summary>
    /// 单车贴图的独立资产名。不用原版 Animals\horse，避免被玩家的马替换 mod（CP 改该全局资产）波及；
    /// 贴图经 AssetRequested 从 mod 目录提供，CP 侧日后也可按此资产名 EditImage 替换单车外观。
    /// </summary>
    private const string BikeTextureAsset = "RedPandaBazaar/Animals/SharedBike";

    /// <summary>单车贴图在 mod 目录内的相对路径。</summary>
    private const string BikeTexturePath = "assets/Textures/RedPandaBazaar_SharedBike.png";

    /// <summary>骑行脚步声 cue：经 Data/AudioChanges 注册，替代马蹄声。</summary>
    private const string BikeStepCueId = "RedPandaBazaar_SharedBikeStep";

    /// <summary>骑行脚步声 wav 在 mod 目录内的相对路径。</summary>
    private const string BikeStepSoundPath = "assets/Audio/RedPandaBazaar_SharedBike_Step.wav";

    /// <summary>项目开通费用。</summary>
    public const int UnlockPrice = 10000;

    /// <summary>存档数据 key。</summary>
    private const string SaveKey = StatsKeys.SharedBikesSave;

    /// <summary>验证用生成坐标与初始朝向（方向编码 0 上 / 1 右 / 2 下 / 3 左）；需求：车辆统一朝左。</summary>
    private static readonly (int X, int Y, int Facing)[] TestTiles = { (21, 34, 3), (22, 34, 3) };

    private static SharedBikesSaveData Data { get; set; } = new();

    /// <summary>客机不读写存档，靠主机广播保持同步。</summary>
    private static bool? _clientUnlocked;

    /// <summary>
    /// 注册内容资产（贴图 + 音效 cue）。必须在 <see cref="ModEntry"/> 的 Entry 阶段调用：
    /// 游戏启动早期就会加载 Data/AudioChanges，AssetRequested 订阅晚了 e.Edit 不会被执行
    /// （Content Patcher 同样是在 Entry 订阅该事件）。
    /// </summary>
    public static void RegisterContent()
    {
        ModInfo.Helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    /// <summary>订阅世界事件。</summary>
    public static void Init()
    {
        Tools.Log("SharedBikes Initializing.");
        ModInfo.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        ModInfo.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        ModInfo.Helper.Events.GameLoop.Saving += OnSaving;
        ModInfo.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        ModInfo.Helper.Events.World.NpcListChanged += OnNpcListChanged;
        ModInfo.Helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        Tools.Log("SharedBikes Inited.");
    }

    /// <summary>
    /// 压制闲置单车的随机待机动画：原版空闲马每帧 0.2% 概率播放甩头/甩尾动画，播完帧回退会破坏固定朝向的摆放要求。
    /// 只处理当前地图（其他地图的实体不 update，不会启动动画），骑乘中的动画不受影响。
    /// </summary>
    private static void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Game1.currentLocation == null)
            return;

        foreach (NPC npc in Game1.currentLocation.characters)
        {
            if (npc is not Horse { rider: null } bike
                || !bike.modData.ContainsKey(BikeMarkerKey)
                || bike.Sprite?.CurrentAnimation == null)
                continue;

            bike.Sprite.StopAnimation();
            // 强制按朝向重设显示帧，纠正待机动画可能留下的错位帧
            bike.Sprite.faceDirection(bike.FacingDirection);
        }
    }

    /// <summary>共享单车项目是否已开通（主机读存档数据，客机读广播缓存）。</summary>
    public static bool IsUnlocked() => Context.IsMainPlayer ? Data.Unlocked : (_clientUnlocked ?? false);

    /// <summary>
    /// 开通共享单车项目：主机直接扣税款执行，客机发请求由主机代扣。
    /// 供银行投资菜单调用；跨存档状态由本 Feature 自治管理。
    /// </summary>
    public static void TryUnlock()
    {
        if (Context.IsMainPlayer)
        {
            if (ExecuteUnlock(Game1.player.UniqueMultiplayerID))
                BroadcastSyncData();
            else
                NotifyTaxNotEnough();
        }
        else
        {
            ModInfo.Helper.Multiplayer.SendMessage(
                new SharedBikesActionRequestData { Action = "unlock" },
                MPMessageType.SharedBikes_ActionRequest,
                modIDs: new[] { ModInfo.ModManifest.UniqueID },
                playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
        }
    }

    /// <summary>主机执行开通：从税款池支出、置位并立即生成单车（幂等：已开通直接返回成功）。</summary>
    private static bool ExecuteUnlock(long playerId)
    {
        if (Data.Unlocked) return true;

        // 开通费是公共支出：从玩家商店的税款池扣，不动操作者个人金币
        if (!PlayerStall.PlayerStall.TrySpendTax(UnlockPrice))
            return false;

        Data.Unlocked = true;
        SpawnTestBikes();
        if (Game1.GetPlayer(playerId)?.IsLocalPlayer == true)
            Game1.addHUDMessage(new HUDMessage(
                Tools.GetI18n(I18nKeys.SharedBikes_UnlockSuccess).ToString(), HUDMessage.achievement_type));
        return true;
    }

    /// <summary>税款池余额不足提示（本地 HUD）。</summary>
    private static void NotifyTaxNotEnough()
    {
        Game1.addHUDMessage(new HUDMessage(
            Tools.GetI18n(I18nKeys.SharedBikes_TaxNotEnough).ToString(), HUDMessage.error_type));
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        Data = new SharedBikesSaveData();
        _clientUnlocked = null;
        if (Context.IsOnHostComputer)
            Data = ModInfo.Helper.Data.ReadSaveData<SharedBikesSaveData>(SaveKey) ?? new SharedBikesSaveData();
    }

    private static void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            ModInfo.Helper.Data.WriteSaveData(SaveKey, Data);
    }

    /// <summary>主机广播开通状态给全部玩家。</summary>
    private static void BroadcastSyncData()
    {
        ModInfo.Helper.Multiplayer.SendMessage(
            new SharedBikesSyncData { Unlocked = Data.Unlocked },
            MPMessageType.SharedBikes_SyncData,
            modIDs: new[] { ModInfo.ModManifest.UniqueID });
    }

    private static void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != ModInfo.ModManifest.UniqueID) return;

        switch (e.Type)
        {
            case MPMessageType.SharedBikes_SyncData:
                if (Context.IsMainPlayer) break;
                var syncData = e.ReadAs<SharedBikesSyncData>();
                if (syncData != null)
                {
                    var wasUnlocked = _clientUnlocked;
                    _clientUnlocked = syncData.Unlocked;
                    // 客机操作者经广播得知开通成功（主机已在主机侧提示过）
                    if (wasUnlocked != true && _clientUnlocked.Value)
                        Game1.addHUDMessage(new HUDMessage(
                            Tools.GetI18n(I18nKeys.SharedBikes_UnlockSuccess).ToString(), HUDMessage.achievement_type));
                }
                break;

            case MPMessageType.SharedBikes_ActionRequest:
                if (!Context.IsMainPlayer) break;
                var request = e.ReadAs<SharedBikesActionRequestData>();
                if (request == null) break;
                switch (request.Action)
                {
                    case "unlock":
                        if (ExecuteUnlock(e.FromPlayerID))
                        {
                            BroadcastSyncData();
                        }
                        else
                        {
                            // 扣款失败（税款池不足）单独回执给请求方，状态广播不带失败语义
                            ModInfo.Helper.Multiplayer.SendMessage(
                                new SharedBikesActionResultData { Success = false },
                                MPMessageType.SharedBikes_ActionResult,
                                modIDs: new[] { ModInfo.ModManifest.UniqueID },
                                playerIDs: new[] { e.FromPlayerID });
                        }
                        break;
                }
                break;

            case MPMessageType.SharedBikes_ActionResult:
                if (Context.IsMainPlayer) break;
                var result = e.ReadAs<SharedBikesActionResultData>();
                if (result is { Success: false })
                    NotifyTaxNotEnough();
                break;
        }
    }

    /// <summary>向游戏内容管线注册单车贴图资产与音效 cue（与原版 Animals\horse、原版音效库完全隔离）。</summary>
    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(BikeTextureAsset))
        {
            e.LoadFromModFile<Texture2D>(BikeTexturePath, AssetLoadPriority.Medium);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
        {
            e.Edit(editor =>
            {
                editor.AsDictionary<string, AudioCueData>().Data[BikeStepCueId] = new AudioCueData
                {
                    Id = BikeStepCueId,
                    Category = "Footsteps",
                    FilePaths = new List<string> { Path.Combine(ModInfo.Helper.DirectoryPath, "assets", Path.GetFileName(BikeStepSoundPath)) }
                };
            });
        }
    }

    /// <summary>客机端收到 netcode 同步来的单车实体时，构造函数只会挂默认马贴图，这里补换。</summary>
    private static void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
    {
        if (e.Added == null)
            return;
        foreach (NPC npc in e.Added)
        {
            if (npc is Horse horse && horse.modData.ContainsKey(BikeMarkerKey))
                ApplyBikeSprite(horse);
        }
    }

    /// <summary>把单车实体换成独立贴图（幂等：已是目标资产则跳过）。</summary>
    private static void ApplyBikeSprite(Horse horse)
    {
        try
        {
            if (horse.Sprite?.spriteTexture?.Name?.Equals(BikeTextureAsset, StringComparison.OrdinalIgnoreCase) == true)
                return;

            horse.Sprite = new AnimatedSprite(BikeTextureAsset, 0, 32, 32)
            {
                textureUsesFlippedRightForLeft = true,
                loop = true
            };
            // 重建的 Sprite 帧号归 0（朝下帧），与已有朝向脱节，按朝向重设显示帧
            horse.Sprite.faceDirection(horse.FacingDirection);
        }
        catch (Exception ex)
        {
            Tools.LogErr($"SharedBikes: 单车贴图更换失败：{ex}");
        }
    }

    /// <summary>每天开始：清掉散落各图的旧坐骑，已开通则在枫叶桥固定坐标生成两匹，并向客机广播状态。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 生成 NPC 属于改世界，仅主机执行；客机屏幕上的实体由 netcode 自动同步
        if (!Context.IsMainPlayer)
            return;

        try
        {
            RemoveAllBikes();
            if (Data.Unlocked)
                SpawnTestBikes();
            BroadcastSyncData();
        }
        catch (Exception ex)
        {
            Tools.LogErr($"SharedBikes: 验证坐骑生成失败：{ex}");
        }
    }

    /// <summary>全图清理验证坐骑——车可能被骑到枫叶桥之外的地图，不能只扫一处。</summary>
    private static void RemoveAllBikes()
    {
        Utility.ForEachLocation(location =>
        {
            location.characters.RemoveWhere(npc => npc is Horse horse && horse.modData.ContainsKey(BikeMarkerKey));
            return true;
        });
    }

    /// <summary>在枫叶桥固定坐标生成验证坐骑。</summary>
    private static void SpawnTestBikes()
    {
        GameLocation? loc = FindMapleBridge();
        if (loc == null)
        {
            Tools.LogWarn("SharedBikes: 未找到地图 Custom_MapleBridge，本轮生成跳过。");
            return;
        }

        foreach ((int x, int y, int facing) in TestTiles)
        {
            var bike = new Horse(Guid.NewGuid(), x, y);
            bike.modData[BikeMarkerKey] = "1";
            // Horse 构造默认 faceDirection(3) 朝左，按桩位给定初始朝向（facingDirection 是 netcode 字段，主机设置即全端同步）
            bike.faceDirection(facing);
            // Horse 构造默认把 currentLocation 指向玩家当前地图，这里改挂到枫叶桥
            bike.currentLocation = loc;
            loc.characters.Add(bike);
            ApplyBikeSprite(bike);
            // onFootstepAction 是原版公开扩展点（Horse.cs:41），直接换掉马蹄声，零补丁
            bike.onFootstepAction = _ => PerformBikeFootstep(bike);
        }
        Tools.LogInfo($"SharedBikes: 已在 Custom_MapleBridge 生成 {TestTiles.Length} 匹验证坐骑。");
    }

    /// <summary>在已实例化的地图中查找枫叶桥；找不到返回 null，不强制实例化。</summary>
    private static GameLocation? FindMapleBridge()
    {
        GameLocation? found = null;
        Utility.ForEachLocation(location =>
        {
            if (location.Name != MapleBridgeLocation)
                return true;
            found = location;
            return false;
        });
        return found;
    }

    /// <summary>骑行脚步声：原版按脚下材质播马蹄声，这里统一换成自行车棘轮声。</summary>
    private static void PerformBikeFootstep(Horse bike)
    {
        try
        {
            Farmer? rider = bike.rider;
            if (rider == null || !rider.ShouldHandleAnimationSound())
                return;
            rider.currentLocation.localSound(BikeStepCueId, bike.Tile);
        }
        catch (Exception ex)
        {
            Tools.LogErr($"SharedBikes: 播放骑行音效失败：{ex}");
        }
    }
}
