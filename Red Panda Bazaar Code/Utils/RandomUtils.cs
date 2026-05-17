using StardewValley;

namespace Red_Panda_Bazaar_Code.Utils;

public static class RandomUtils
{
    // ====== 不可复现随机数（基于 Game1.random，每次不同） ======
    public static int Next() => Game1.random.Next();
    public static int Next(int maxValue) => Game1.random.Next(maxValue);
    public static int Next(int minValue, int maxValue) => Game1.random.Next(minValue, maxValue);
    public static double NextDouble() => Game1.random.NextDouble();

    /// <summary>可复现随机数生成器。同一天同上下文的随机序列完全一致。</summary>
    public class RandomSeed : Random
    {
        /// <summary>基于存档ID、天数、上下文创建每日确定性随机数生成器。</summary>
        public RandomSeed(string context) : base(MakeSeed(context)) { }

        /// <summary>使用自定义种子创建。</summary>
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