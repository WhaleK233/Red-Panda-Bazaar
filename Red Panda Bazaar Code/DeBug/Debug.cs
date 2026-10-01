using Red_Panda_Bazaar_Code.Compatibility;

namespace Red_Panda_Bazaar_Code.DeBug;

/// <summary>
/// 调试的通用部分（可视化开关、Tile Action 高亮、调试菜单、光标传送）已拆分到独立模组
/// Dev Debug Kit（WhaleK233.DevDebugKit），调试模式的开关状态由它持有。
/// 这里只保留本模组专属调试 UI（如摊位菜单的 Action ID 叠层）对开关状态的查询入口；
/// 未安装 Dev Debug Kit 时恒为 false，本模组正常工作。
/// </summary>
public static class Debug {
    public static bool IsEnabled => Integrations.Installed.DevDebugKitApi?.IsEnabled ?? false;
}
