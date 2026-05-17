using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

public static class TimeUtils
{
    public static bool IsDayTime(GameLocation loc)
    {
        return Game1.timeOfDay < Game1.getStartingToGetDarkTime(loc);
    }

    public static bool IsDuskTime(GameLocation loc)
    {
        return !IsDayTime(loc) && !IsNightTime(loc);
    }

    public static bool IsNightTime(GameLocation loc)
    {
        return Game1.timeOfDay > Game1.getTrulyDarkTime(loc);
    }
}
