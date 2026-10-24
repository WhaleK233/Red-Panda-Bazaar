namespace Red_Panda_Bazaar_Code.Features.SharedBikes;

/// <summary>存档数据（仅主机读写）。</summary>
public class SharedBikesSaveData
{
    /// <summary>共享单车项目是否已开通。</summary>
    public bool Unlocked { get; set; }
}

/// <summary>多人同步用的包裹，避免直接暴露内部数据类结构。</summary>
public class SharedBikesSyncData
{
    public bool Unlocked { get; set; }

    /// <summary>静默同步：只更新状态缓存，不弹「开通成功」的 HUD（日更广播与中途加入的补发用）。</summary>
    public bool Silent { get; set; }
}

/// <summary>客机→主机的操作请求数据。</summary>
public class SharedBikesActionRequestData
{
    public string Action { get; set; } = "";
}

/// <summary>主机→请求方客机的操作结果。</summary>
public class SharedBikesActionResultData
{
    public bool Success { get; set; }
}
