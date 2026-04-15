using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MarketDeckManager : NetworkBehaviour
{
    public static MarketDeckManager Instance { get; private set; }

    public readonly NetworkList<int> Tier1VisibleIds = new NetworkList<int>();
    public readonly NetworkList<int> Tier2VisibleIds = new NetworkList<int>();
    public readonly NetworkList<int> Tier3VisibleIds = new NetworkList<int>();
    public NetworkVariable<int> Tier1DeckRemaining = new NetworkVariable<int>(0);
    public NetworkVariable<int> Tier2DeckRemaining = new NetworkVariable<int>(0);
    public NetworkVariable<int> Tier3DeckRemaining = new NetworkVariable<int>(0);

    private Queue<int> tier1Deck = new Queue<int>();
    private Queue<int> tier2Deck = new Queue<int>();
    private Queue<int> tier3Deck = new Queue<int>();

    private const int FaceUpPerTier = 4;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Tier1VisibleIds.OnListChanged += OnVisibleListChanged;
        Tier2VisibleIds.OnListChanged += OnVisibleListChanged;
        Tier3VisibleIds.OnListChanged += OnVisibleListChanged;

        if (IsServer)
        {
            GameEvents.OnServerCardBought += HandleServerCardBought;
            InitializeDecksAndMarket();
        }

        RefreshMarketUIFromNetworkState();
    }

    public override void OnNetworkDespawn()
    {
        Tier1VisibleIds.OnListChanged -= OnVisibleListChanged;
        Tier2VisibleIds.OnListChanged -= OnVisibleListChanged;
        Tier3VisibleIds.OnListChanged -= OnVisibleListChanged;

        if (IsServer)
        {
            GameEvents.OnServerCardBought -= HandleServerCardBought;
        }
    }

    private void InitializeDecksAndMarket()
    {
        if (GlobalCardDatabase.Instance == null)
        {
            Debug.LogError("[MarketDeck] GlobalCardDatabase 未初始化，无法创建牌堆。");
            return;
        }

        tier1Deck = GlobalCardDatabase.Instance.CreateShuffledDeckIds(1);
        tier2Deck = GlobalCardDatabase.Instance.CreateShuffledDeckIds(2);
        tier3Deck = GlobalCardDatabase.Instance.CreateShuffledDeckIds(3);

        Tier1VisibleIds.Clear();
        Tier2VisibleIds.Clear();
        Tier3VisibleIds.Clear();

        FillFaceUp(Tier1VisibleIds, tier1Deck, FaceUpPerTier);
        FillFaceUp(Tier2VisibleIds, tier2Deck, FaceUpPerTier);
        FillFaceUp(Tier3VisibleIds, tier3Deck, FaceUpPerTier);
        SyncDeckRemaining();

        Debug.Log("[MarketDeck] 市场明牌初始化完成。");
    }

    private void HandleServerCardBought(int cardId)
    {
        CardSO card = GlobalCardDatabase.Instance != null ? GlobalCardDatabase.Instance.GetCard(cardId) : null;
        if (card == null) return;

        switch (card.level)
        {
            case 1:
                ReplaceBoughtCard(Tier1VisibleIds, tier1Deck, cardId);
                break;
            case 2:
                ReplaceBoughtCard(Tier2VisibleIds, tier2Deck, cardId);
                break;
            case 3:
                ReplaceBoughtCard(Tier3VisibleIds, tier3Deck, cardId);
                break;
            default:
                Debug.LogWarning($"[MarketDeck] 未知卡牌层级: {card.level}, cardId: {cardId}");
                break;
        }

        SyncDeckRemaining();
    }

    public bool IsCardVisible(int cardId)
    {
        return ContainsCard(Tier1VisibleIds, cardId)
            || ContainsCard(Tier2VisibleIds, cardId)
            || ContainsCard(Tier3VisibleIds, cardId);
    }

    private static void FillFaceUp(NetworkList<int> visible, Queue<int> deck, int targetCount)
    {
        while (visible.Count < targetCount && deck.Count > 0)
        {
            visible.Add(deck.Dequeue());
        }
    }

    private static void ReplaceBoughtCard(NetworkList<int> visible, Queue<int> deck, int cardId)
    {
        int boughtIndex = -1;
        for (int i = 0; i < visible.Count; i++)
        {
            if (visible[i] == cardId)
            {
                boughtIndex = i;
                break;
            }
        }

        if (boughtIndex < 0)
        {
            return;
        }

        if (deck.Count > 0)
        {
            visible[boughtIndex] = deck.Dequeue();
        }
        else
        {
            visible.RemoveAt(boughtIndex);
        }
    }

    private void SyncDeckRemaining()
    {
        if (!IsServer) return;

        Tier1DeckRemaining.Value = tier1Deck.Count;
        Tier2DeckRemaining.Value = tier2Deck.Count;
        Tier3DeckRemaining.Value = tier3Deck.Count;
    }

    private void OnVisibleListChanged(NetworkListEvent<int> _)
    {
        RefreshMarketUIFromNetworkState();
    }

    private void RefreshMarketUIFromNetworkState()
    {
        if (MarketManager.Instance == null || GlobalCardDatabase.Instance == null) return;

        List<CardSO> cards = new List<CardSO>(
            Tier1VisibleIds.Count + Tier2VisibleIds.Count + Tier3VisibleIds.Count
        );

        AppendCards(cards, Tier1VisibleIds);
        AppendCards(cards, Tier2VisibleIds);
        AppendCards(cards, Tier3VisibleIds);

        MarketManager.Instance.UpdateMarket(cards);
    }

    private static void AppendCards(List<CardSO> target, NetworkList<int> ids)
    {
        if (GlobalCardDatabase.Instance == null) return;

        for (int i = 0; i < ids.Count; i++)
        {
            CardSO card = GlobalCardDatabase.Instance.GetCard(ids[i]);
            if (card != null)
            {
                target.Add(card);
            }
        }
    }

    private static bool ContainsCard(NetworkList<int> ids, int cardId)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == cardId)
            {
                return true;
            }
        }

        return false;
    }
}
