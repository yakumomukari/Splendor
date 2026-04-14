using System.Collections.Generic;
using UnityEngine;

public class GlobalCardDatabase : MonoBehaviour
{
    public static GlobalCardDatabase Instance { get; private set; }

    private Dictionary<int, CardSO> idToCard = new Dictionary<int, CardSO>();
    private readonly List<CardSO> tier1Cards = new List<CardSO>();
    private readonly List<CardSO> tier2Cards = new List<CardSO>();
    private readonly List<CardSO> tier3Cards = new List<CardSO>();

    private const int Tier1TargetCount = 40;
    private const int Tier2TargetCount = 30;
    private const int Tier3TargetCount = 20;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        // 一次性把文件夹里所有的 ScriptableObject 全扒出来存进字典
        CardSO[] allCards = Resources.LoadAll<CardSO>("Cards");
        foreach (var card in allCards)
        {
            if (!idToCard.ContainsKey(card.id))
            {
                idToCard.Add(card.id, card);
                ClassifyTier(card);
            }
            else
            {
                Debug.LogError($"[致命错误] 卡牌 ID 冲突！ID: {card.id} 被多次使用！");
            }
        }

        ValidateTierCounts();
        Debug.Log($"[数据库] 本地卡牌数据加载完毕，共加载 {idToCard.Count} 张卡牌。L1:{tier1Cards.Count} L2:{tier2Cards.Count} L3:{tier3Cards.Count}");
    }

    public CardSO GetCard(int id)
    {
        if (idToCard.TryGetValue(id, out CardSO card)) return card;
        Debug.LogError($"[数据库] 试图查询不存在的卡牌 ID: {id}");
        return null;
    }

    public List<CardSO> GetTierCards(int level)
    {
        switch (level)
        {
            case 1: return new List<CardSO>(tier1Cards);
            case 2: return new List<CardSO>(tier2Cards);
            case 3: return new List<CardSO>(tier3Cards);
            default:
                Debug.LogWarning($"[数据库] 请求了未知层级: {level}");
                return new List<CardSO>();
        }
    }

    public Queue<int> CreateShuffledDeckIds(int level)
    {
        List<CardSO> source = GetTierCards(level);
        ShuffleInPlace(source);

        Queue<int> deck = new Queue<int>(source.Count);
        foreach (var card in source)
        {
            deck.Enqueue(card.id);
        }
        return deck;
    }

    private void ClassifyTier(CardSO card)
    {
        if (card == null) return;

        if (TryGetTierFromName(card.name, out int tierFromName))
        {
            switch (tierFromName)
            {
                case 1: tier1Cards.Add(card); return;
                case 2: tier2Cards.Add(card); return;
                case 3: tier3Cards.Add(card); return;
            }
            return;
        }

        // 兼容: 命名未遵循约定时，退回到 level 字段。
        switch (card.level)
        {
            case 1: tier1Cards.Add(card); break;
            case 2: tier2Cards.Add(card); break;
            case 3: tier3Cards.Add(card); break;
            default:
                Debug.LogWarning($"[数据库] 卡牌 {card.name} (ID:{card.id}) 层级无法识别，已忽略。请检查命名前缀或level字段。");
                break;
        }
    }

    private static bool TryGetTierFromName(string cardName, out int tier)
    {
        tier = 0;
        if (string.IsNullOrEmpty(cardName)) return false;

        // 支持: "1_xxx"、"2_xxx"、"3_xxx"
        char firstChar = cardName[0];
        if (firstChar == '1' || firstChar == '2' || firstChar == '3')
        {
            tier = firstChar - '0';
            return true;
        }

        // 支持: "Card_101" 这类命名
        int underscoreIndex = cardName.IndexOf('_');
        if (underscoreIndex >= 0 && underscoreIndex + 1 < cardName.Length)
        {
            char digit = cardName[underscoreIndex + 1];
            if (digit == '1' || digit == '2' || digit == '3')
            {
                tier = digit - '0';
                return true;
            }
        }

        return false;
    }

    private void ValidateTierCounts()
    {
        if (tier1Cards.Count != Tier1TargetCount)
        {
            Debug.LogWarning($"[数据库] 一级卡数量异常，期望 {Tier1TargetCount}，实际 {tier1Cards.Count}。");
        }

        if (tier2Cards.Count != Tier2TargetCount)
        {
            Debug.LogWarning($"[数据库] 二级卡数量异常，期望 {Tier2TargetCount}，实际 {tier2Cards.Count}。");
        }

        if (tier3Cards.Count != Tier3TargetCount)
        {
            Debug.LogWarning($"[数据库] 三级卡数量异常，期望 {Tier3TargetCount}，实际 {tier3Cards.Count}。");
        }
    }

    private static void ShuffleInPlace(List<CardSO> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CardSO tmp = cards[i];
            cards[i] = cards[j];
            cards[j] = tmp;
        }
    }
}