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

    /// <summary>
    /// 校验玩家拿取代币的操作是否合法 (完美适配无弃牌阶段的动态上限)
    /// </summary>
    /// <param name="selectedTokens">玩家选择拿取的数量数组</param>
    /// <param name="bankRemaining">银行当前的剩余库存</param>
    /// <param name="playerCurrentTotal">玩家当前兜里已经有的代币总数 (含黄金)</param>
    public static bool IsValidTokenDraft(int[] selectedTokens, int[] bankRemaining, int playerCurrentTotal)
    {
        if (selectedTokens.Length != 5 || bankRemaining.Length != 5) return false;

        int totalSelected = 0;
        bool hasDouble = false;
        int availableColorsInBank = 0; // 记录银行当前还有几种颜色是有货的

        for (int i = 0; i < 5; i++)
        {
            if (bankRemaining[i] > 0) availableColorsInBank++;

            int count = selectedTokens[i];
            if (count < 0) return false; // 防穿透
            if (count > bankRemaining[i]) return false; // 银行没那么多

            if (count == 2)
            {
                hasDouble = true;
                if (bankRemaining[i] < 4) return false; // 拿2个同色的铁律
            }
            else if (count > 2)
            {
                return false; // 绝对不允许单色拿超过2个
            }

            totalSelected += count;
        }

        if (totalSelected <= 0) return false;

        // 绝对底线：拿完之后总数不能超过 10
        if (playerCurrentTotal + totalSelected > 10) return false;

        // 分支判定：拿同色 vs 拿不同色
        if (hasDouble)
        {
            // 如果触发了拿2个同色，那总拿取量必须严格等于 2
            if (totalSelected != 2) return false;
        }
        else
        {
            // 拿不同色的情况：
            // 正常应该拿 3 个。但是如果玩家快满 10 个了，或者银行没颜色了，就只能被迫少拿。
            // 必须强制玩家拿“允许范围内的最大值”，不能故意少拿。
            int requiredToTake = Math.Min(3, 10 - playerCurrentTotal);
            requiredToTake = Math.Min(requiredToTake, availableColorsInBank);

            if (totalSelected != requiredToTake) return false;
        }

        return true;
    }
}
