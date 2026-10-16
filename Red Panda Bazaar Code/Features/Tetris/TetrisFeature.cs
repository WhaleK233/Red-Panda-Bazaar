using StardewModdingAPI;
using StardewValley;
using Red_Panda_Bazaar_Code.Utils;

namespace Red_Panda_Bazaar_Code.Features.Tetris;

/// <summary>俄罗斯方块小游戏:纯本地玩法,控制台命令 rpab_tetris 启动。</summary>
public static class TetrisFeature
{
    public static void Init()
    {
        Tools.Log("TetrisFeature Initializing.");
        ModInfo.Helper.ConsoleCommands.Add("rpab_tetris", "打开小熊猫集市俄罗斯方块小游戏。", OnCommand);
        Tools.Log("TetrisFeature Inited.");
    }

    private static void OnCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Tools.LogInfo("无法在读取存档前打开俄罗斯方块。");
            return;
        }
        Game1.activeClickableMenu = new TetrisMenu();
    }
}
