using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

public static class RandomUtils
{
    public static int Next() => Game1.random.Next();

    public static int Next(int maxValue) => Game1.random.Next(maxValue);

    public static int Next(int minValue, int maxValue) => Game1.random.Next(minValue, maxValue);

    public static double NextDouble() => Game1.random.NextDouble();

    /// <summary>可复现的每日随机数生成器。</summary>
    public class RandomSeed : Random
    {
        public RandomSeed(string context) : base(MakeSeed(context)) { }

        public RandomSeed(int seed) : base(seed) { }

        private static int MakeSeed(string context)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (int)(Game1.uniqueIDForThisGame & 0xFFFFFFFF);
                hash = hash * 31 + (int)(Game1.uniqueIDForThisGame >> 32);
                hash = hash * 31 + (int)Game1.stats.DaysPlayed;
                foreach (var c in context)
                    hash = hash * 31 + c;
                return hash;
            }
        }
    }
}
