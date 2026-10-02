using System;
using System.Collections.Generic;
using System.IO;
using WhaleK.Common;
using Microsoft.Xna.Framework.Graphics;
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

    /// <summary>验证用生成坐标。</summary>
    private static readonly (int X, int Y)[] TestTiles = { (21, 34), (22, 34) };

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
        ModInfo.Helper.Events.World.NpcListChanged += OnNpcListChanged;
        Tools.Log("SharedBikes Inited.");
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
        }
        catch (Exception ex)
        {
            Tools.LogErr($"SharedBikes: 单车贴图更换失败：{ex}");
        }
    }

    /// <summary>每天开始：清掉散落各图的旧验证坐骑，再在枫叶桥固定坐标生成两匹。</summary>
    private static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        // 生成 NPC 属于改世界，仅主机执行；客机屏幕上的实体由 netcode 自动同步
        if (!Context.IsMainPlayer)
            return;

        try
        {
            RemoveAllBikes();
            SpawnTestBikes();
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

        foreach ((int x, int y) in TestTiles)
        {
            var bike = new Horse(Guid.NewGuid(), x, y);
            bike.modData[BikeMarkerKey] = "1";
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
