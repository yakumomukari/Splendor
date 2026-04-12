using System.Collections.Generic;
using UnityEngine;

public class MarketTester : MonoBehaviour
{
    [Header("核心引用")]
    public MarketManager marketManager;
    
    [Header("模拟发牌 (把生成的CardSO拖进来)")]
    public List<CardSO> testCards;

    [Header("模拟玩家资源 (顺序:白,蓝,绿,红,黑)")]
    public int[] playerTokens = new int[5] { 0, 0, 0, 0, 0 };
    public int[] playerDiscounts = new int[5] { 0, 0, 0, 0, 0 };
    public int playerGold = 0;

    void Start()
    {
        // 1. 游戏开始时，通知 MarketManager 将填入的测试卡牌生成到桌面上
        if (marketManager != null && testCards != null)
        {
            marketManager.UpdateMarket(testCards);
        }
    }

    void Update()
    {
        // 2. 每帧实时刷新卡牌的交互状态。
        // 这样你在运行环境 (Play Mode) 中去修改上面定义的 playerTokens 等数值，就能实时看到卡牌上的按钮变亮/变灰。
        if (marketManager != null)
        {
            marketManager.SetMarketInteractable(playerTokens, playerDiscounts, playerGold);
        }
    }
}
