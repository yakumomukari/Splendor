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

    [Header("Deck UI Bindings")]
    public DeckUI deckUI_T1;
    public DeckUI deckUI_T2;
    public DeckUI deckUI_T3;

    // 用于缓存生成的 CardUI 以及它所对应的 CardSO 数据
    private List<CardUI> activeCardUIs = new List<CardUI>();
    private Dictionary<int, CardSO> cardDataMap = new Dictionary<int, CardSO>();

    /// <summary>
    /// 更新场上的卡牌展示，同时更新三排牌堆的剩余数量
    /// </summary>
    /// <param name="cards">需要展示在场上的卡牌列表</param>
    /// <param name="t1Count">Level 1 牌堆剩余数量</param>
    /// <param name="t2Count">Level 2 牌堆剩余数量</param>
    /// <param name="t3Count">Level 3 牌堆剩余数量</param>
    public void UpdateMarket(List<CardSO> cards, int t1Count, int t2Count, int t3Count)
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

        // 更新牌堆剩余数量显示
        if (deckUI_T1 != null) deckUI_T1.UpdateCount(t1Count);
        if (deckUI_T2 != null) deckUI_T2.UpdateCount(t2Count);
        if (deckUI_T3 != null) deckUI_T3.UpdateCount(t3Count);

        TryRefreshMarketInteractable();
    }

    // 缓存本地玩家的指针，O(1) 访问，告别 GetComponent
    private Player localPlayerCache;
    private bool turnEventsBound;

    // 留给 Player 实体主动上门注册的接口
    public void RegisterLocalPlayer(Player player)
    {
        if (localPlayerCache == player)
        {
            TryRefreshMarketInteractable();
            return;
        }

        UnbindLocalPlayerEvents();
        localPlayerCache = player;

        if (localPlayerCache != null)
        {
            Debug.Log("[Market] 本地玩家数据已接入市场UI！");
            localPlayerCache.Tokens.OnValueChanged += OnLocalPlayerTokensChanged;
            localPlayerCache.Discounts.OnValueChanged += OnLocalPlayerDiscountsChanged;
        }

        TryRefreshMarketInteractable();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        TryBindTurnEvents();
        TryRefreshMarketInteractable();
    }

    private void Update()
    {
        if (!turnEventsBound)
        {
            TryBindTurnEvents();
        }
    }

    private void OnDisable()
    {
        UnbindTurnEvents();
        UnbindLocalPlayerEvents();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void TryRefreshMarketInteractable()
    {
        if (localPlayerCache == null) return;

        SetMarketInteractable(
            localPlayerCache.Tokens.Value.ToBaseGemArray(),
            localPlayerCache.Discounts.Value.ToBaseGemArray(),
            localPlayerCache.Tokens.Value.Gold
        );
    }

    // 3. 【新增】供服务器调用的查询接口
    public CardSO GetCardById(int id)
    {
        if (cardDataMap.ContainsKey(id)) return cardDataMap[id];
        Debug.LogError($"[Market] 找不到 ID 为 {id} 的卡牌！");
        return null;
    }

    // 4. 【修改】引入回合判定逻辑
    public void SetMarketInteractable(int[] playerTokens, int[] playerDiscounts, int playerGold)
    {
        // 极限防雷：确保网络组件和状态机都已经活过来了
        bool isMyTurn = false;
        bool isBlockedByReturn = false;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient && TurnManager.Instance != null)
        {
            isMyTurn = TurnManager.Instance.CurrentActivePlayerId.Value == NetworkManager.Singleton.LocalClientId;
            isBlockedByReturn = TurnManager.Instance.IsWaitingForReturn.Value;
        }

        foreach (var cardUI in activeCardUIs)
        {
            if (cardUI == null) continue;

            int cardId = cardUI.GetCardId(); // 记得去 CardUI 里加这个 Getter
            if (!cardDataMap.TryGetValue(cardId, out CardSO data) || data == null)
            {
                cardUI.SetState(false);
                continue;
            }

            int[] cardCosts = new int[] { data.costWhite, data.costBlue, data.costGreen, data.costRed, data.costBlack };

            // 只有【钱够】且【是我的回合】，按钮才亮起
            bool canAfford = GameRules.CanAffordCard(playerTokens, playerDiscounts, playerGold, cardCosts, out _);
            cardUI.SetState(canAfford && isMyTurn && !isBlockedByReturn);
        }
    }
    // 市场移除与补牌由 MarketDeckManager 的网络状态驱动，不在本地直接删卡。
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

    private void OnLocalPlayerTokensChanged(TokenAssets _, TokenAssets __)
    {
        TryRefreshMarketInteractable();
    }

    private void OnLocalPlayerDiscountsChanged(TokenAssets _, TokenAssets __)
    {
        TryRefreshMarketInteractable();
    }

    private void TryBindTurnEvents()
    {
        if (turnEventsBound) return;
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.CurrentActivePlayerId.OnValueChanged += OnTurnChanged;
        TurnManager.Instance.IsWaitingForReturn.OnValueChanged += OnWaitReturnChanged;
        turnEventsBound = true;
    }

    private void UnbindTurnEvents()
    {
        if (!turnEventsBound) return;
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.CurrentActivePlayerId.OnValueChanged -= OnTurnChanged;
        TurnManager.Instance.IsWaitingForReturn.OnValueChanged -= OnWaitReturnChanged;
        turnEventsBound = false;
    }

    private void OnTurnChanged(ulong _, ulong __)
    {
        TryRefreshMarketInteractable();
    }

    private void OnWaitReturnChanged(bool _, bool __)
    {
        TryRefreshMarketInteractable();
    }

    private void UnbindLocalPlayerEvents()
    {
        if (localPlayerCache == null) return;

        localPlayerCache.Tokens.OnValueChanged -= OnLocalPlayerTokensChanged;
        localPlayerCache.Discounts.OnValueChanged -= OnLocalPlayerDiscountsChanged;
        localPlayerCache = null;
    }
}
