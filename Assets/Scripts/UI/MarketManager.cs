using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode; // 虽然 B 没用，但你要用它来判断本地 ID

public class MarketManager : MonoBehaviour
{
    // 1. 【新增】单例化，方便 Player 脚本在服务器端查卡牌数值
    public static MarketManager Instance { get; private set; }
    [Header("Prefabs & Containers")]
    public CardUI cardPrefab;

    public Transform tier1Parent; // 等级 1 的卡牌父节点
    public Transform tier2Parent; // 等级 2 的卡牌父节点
    public Transform tier3Parent; // 等级 3 的卡牌父节点

    // 用于缓存生成的 CardUI 以及它所对应的 CardSO 数据
    private List<CardUI> activeCardUIs = new List<CardUI>();
    private Dictionary<int, CardSO> cardDataMap = new Dictionary<int, CardSO>();

    /// <summary>
    /// 更新场上的卡牌展示
    /// </summary>
    /// <param name="cards">需要展示在场上的卡牌列表</param>



    // 缓存本地玩家的指针，O(1) 访问，告别 GetComponent
    private Player localPlayerCache;

    // 留给 Player 实体主动上门注册的接口
    public void RegisterLocalPlayer(Player player)
    {
        localPlayerCache = player;
        Debug.Log("[Market] 本地玩家数据已接入市场UI！");

        // 【打补丁】：只要钱包或折扣变了，立刻触发一次市场UI验资！
        localPlayerCache.Tokens.OnValueChanged += (oldVal, newVal) => RefreshMarketUI();
        localPlayerCache.Discounts.OnValueChanged += (oldVal, newVal) => RefreshMarketUI();

        RefreshMarketUI();
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Update()
    {
        // 如果指针为空，直接 return，不产生任何垃圾回收(GC)和寻址开销
        if (localPlayerCache == null) return;

        RefreshMarketUI();
    }
    private void RefreshMarketUI()
    {
        SetMarketInteractable(
            localPlayerCache.Tokens.Value.ToBaseGemArray(),
            localPlayerCache.Discounts.Value.ToBaseGemArray(),
            localPlayerCache.Tokens.Value.Gold
        );
    }

    private void OnEnable()
    {
        // 2. 【新增】监听服务器的买卡/预约成功广播
        GameEvents.OnServerCardBought += RemoveCardFromMarket;
        // GameEvents.OnServerCardReserved += RemoveCardFromMarket;
    }

    private void OnDisable()
    {
        GameEvents.OnServerCardBought -= RemoveCardFromMarket;
    }
    // 3. 【新增】供服务器调用的查询接口
    public CardSO GetCardById(int id)
    {
        if (cardDataMap.ContainsKey(id)) return cardDataMap[id];
        Debug.LogError($"[Market] 找不到 ID 为 {id} 的卡牌！");
        return null;
    }
    public void UpdateMarket(List<CardSO> cards)
    {
        ClearParent(tier1Parent);
        ClearParent(tier2Parent);
        ClearParent(tier3Parent);
        activeCardUIs.Clear();
        cardDataMap.Clear();

        foreach (var card in cards)
        {
            if (card == null) continue;

            Transform targetParent = GetParentByTier(card.level);
            if (targetParent != null)
            {
                CardUI newCardUI = Instantiate(cardPrefab, targetParent);
                newCardUI.Setup(card);

                activeCardUIs.Add(newCardUI);
                cardDataMap.Add(card.id, card); // 记录 ID 与数据的映射
            }
        }
    }

    // 4. 【修改】引入回合判定逻辑
    public void SetMarketInteractable(int[] playerTokens, int[] playerDiscounts, int playerGold)
    {
        // 极限防雷：确保网络组件和状态机都已经活过来了
        bool isMyTurn = false;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && TurnManager.Instance != null)
        {
            isMyTurn = TurnManager.Instance.CurrentActivePlayerId.Value == NetworkManager.Singleton.LocalClientId;
        }
        foreach (var cardUI in activeCardUIs)
        {
            if (cardUI == null) continue;

            int cardId = cardUI.GetCardId(); // 记得去 CardUI 里加这个 Getter
            CardSO data = cardDataMap[cardId];

            int[] cardCosts = new int[] { data.costWhite, data.costBlue, data.costGreen, data.costRed, data.costBlack };

            // 只有【钱够】且【是我的回合】，按钮才亮起
            bool canAfford = GameRules.CanAffordCard(playerTokens, playerDiscounts, playerGold, cardCosts, out _);
            cardUI.SetState(canAfford && isMyTurn);
        }
    }
    // 5. 【新增】物理移除逻辑
    private void RemoveCardFromMarket(int cardId)
    {
        // 找到那个对应的 CardUI 并销毁
        CardUI targetUI = activeCardUIs.Find(x => x.GetCardId() == cardId);
        if (targetUI != null)
        {
            activeCardUIs.Remove(targetUI);
            cardDataMap.Remove(cardId);
            Destroy(targetUI.gameObject);
            Debug.Log($"[Market] 卡牌 {cardId} 已从市场移除。");
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
