using System.Collections.Generic;
using UnityEngine;

public class GlobalCardDatabase : MonoBehaviour
{
    public static GlobalCardDatabase Instance { get; private set; }

    private Dictionary<int, CardSO> idToCard = new Dictionary<int, CardSO>();

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
            }
            else
            {
                Debug.LogError($"[致命错误] 卡牌 ID 冲突！ID: {card.id} 被多次使用！");
            }
        }

        Debug.Log($"[数据库] 本地卡牌数据加载完毕，共加载 {idToCard.Count} 张卡牌。");
    }

    public CardSO GetCard(int id)
    {
        if (idToCard.TryGetValue(id, out CardSO card)) return card;
        Debug.LogError($"[数据库] 试图查询不存在的卡牌 ID: {id}");
        return null;
    }
}