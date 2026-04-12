using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarketManager : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    public CardUI cardPrefab;
    
    public Transform tier1Parent; // 等级 1 的卡牌父节点
    public Transform tier2Parent; // 等级 2 的卡牌父节点
    public Transform tier3Parent; // 等级 3 的卡牌父节点

    // 用于缓存生成的 CardUI 以及它所对应的 CardSO 数据
    private List<CardUI> activeCardUIs = new List<CardUI>();
    private Dictionary<CardUI, CardSO> cardDataMap = new Dictionary<CardUI, CardSO>();

    /// <summary>
    /// 更新场上的卡牌展示
    /// </summary>
    /// <param name="cards">需要展示在场上的卡牌列表</param>
    public void UpdateMarket(List<CardSO> cards)
    {
        // 1. 清空旧卡牌
        ClearParent(tier1Parent);
        ClearParent(tier2Parent);
        ClearParent(tier3Parent);
        activeCardUIs.Clear();
        cardDataMap.Clear();

        // 2. 根据等级生成新卡牌
        foreach (var card in cards)
        {
            if (card == null) continue;

            Transform targetParent = GetParentByTier(card.level);
            if (targetParent != null)
            {
                CardUI newCardUI = Instantiate(cardPrefab, targetParent);
                newCardUI.Setup(card);
                
                // 缓存引用以供后续检查是否可以购买
                activeCardUIs.Add(newCardUI);
                cardDataMap.Add(newCardUI, card);
            }
        }
    }

    /// <summary>
    /// 根据玩家当前的资源状态，更新场上所有卡牌的按钮交互状态（买得起的亮起，买不起的置灰）
    /// </summary>
    public void SetMarketInteractable(int[] playerTokens, int[] playerDiscounts, int playerGold)
    {
        foreach (var cardUI in activeCardUIs)
        {
            if (cardUI == null) continue;

            CardSO data = cardDataMap[cardUI];
            
            // 将 CardSO 中离散的花费字段拼合成数组，以便传入算法 (对应白,蓝,绿,红,黑序列)
            int[] cardCosts = new int[] { data.costWhite, data.costBlue, data.costGreen, data.costRed, data.costBlack };
            
            // 调用核心算法类进行校验
            bool canAfford = GameRules.CanAffordCard(playerTokens, playerDiscounts, playerGold, cardCosts, out int goldNeeded);
            
            // 获取 Button 组件控制交互
            Button btn = cardUI.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = canAfford;
            }
        }
    }

    /// <summary>
    /// 清理指定父节点下的所有子物体
    /// </summary>
    private void ClearParent(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 将卡牌的 Level 映射到对应的父节点
    /// </summary>
    private Transform GetParentByTier(int level)
    {
        switch (level)
        {
            case 1: return tier1Parent;
            case 2: return tier2Parent;
            case 3: return tier3Parent;
            default:
                Debug.LogWarning($"未知的卡牌等级: {level}");
                return null;
        }
    }
}
