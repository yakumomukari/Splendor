using System;

public static class GameRules
{
    /// <summary>
    /// 判断玩家是否买得起某张卡牌
    /// </summary>
    /// <param name="playerGems">玩家拥有的 5 种宝石数量 (顺序建议: 白,蓝,绿,红,黑)</param>
    /// <param name="playerDiscounts">玩家拥有的 5 种永久折扣数量 (顺序建议: 白,蓝,绿,红,黑)</param>
    /// <param name="playerGold">玩家拥有的黄金代币数量</param>
    /// <param name="cardCosts">卡牌的 5 种成本 (顺序: 白,蓝,绿,红,黑)</param>
    /// <param name="goldNeeded">输出：缺少的宝石总量（即需要消耗的黄金数）</param>
    /// <returns>布尔值：是否买得起</returns>
    public static bool CanAffordCard(int[] playerGems, int[] playerDiscounts, int playerGold, int[] cardCosts, out int goldNeeded)
    {
        goldNeeded = 0;

        // 基础验证，确保数组长度必须为 5 (五种基础宝石)
        if (playerGems.Length != 5 || playerDiscounts.Length != 5 || cardCosts.Length != 5)
        {
            throw new ArgumentException("数组长度错误：必须包含 5 种基础宝石的数据。");
        }

        for (int i = 0; i < 5; i++)
        {
            // 实际成本 = Max(0, 成本 - 折扣)
            int actualCost = Math.Max(0, cardCosts[i] - playerDiscounts[i]);
            
            // 缺口 = Max(0, 实际成本 - 持有宝石)
            int shortfall = Math.Max(0, actualCost - playerGems[i]);
            
            // 累加缺口，这也就是需要用黄金抵扣的数量
            goldNeeded += shortfall;
        }

        // 如果黄金储备 >= 总缺口，说明玩家可以通过花费黄金来补足欠缺的宝石
        return playerGold >= goldNeeded;
    }
}
