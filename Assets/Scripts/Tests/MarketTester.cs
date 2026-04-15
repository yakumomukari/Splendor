using System.Collections.Generic;
using UnityEngine;

public class LocalUIDriver : MonoBehaviour
{
    [Header("依赖绑定")]
    public MarketManager marketManager;
    public PlayerPanel playerPanel; // 挂载刚做好的玩家面板预制体
    public BankUI bankUI;           // 挂载刚做好的银行代币面板
    
    [Header("测试数据源")]
    public List<CardSO> testDeck; // 请在 Inspector 中随意拖入 15-20 张卡牌 SO

    private List<CardSO> activeCards = new List<CardSO>();
    
    // 模拟玩家当前资产
    private int[] playerTokens = new int[5];
    private int[] playerDiscounts = new int[5];
    private int playerGold = 0;
    private int playerScore = 0;

    // 模拟的银行数据
    private int[] bankTokens = new int[5] { 7, 7, 7, 7, 7 };
    private int bankGold = 5;

    private void Start()
    {
        // 提取前 12 张卡作为初始市场
        for (int i = 0; i < 12 && i < testDeck.Count; i++)
        {
            activeCards.Add(testDeck[i]);
        }

        // 注册全局事件
        GameEvents.OnBuyCardReq += HandleBuyCardRequest;
        GameEvents.OnTakeTokensReq += HandleTakeTokens;

        RefreshMarket();
        RefreshPlayerPanel();
        RefreshBankPanel();
        PrintCurrentAssets();
    }

    private void OnDestroy()
    {
        // 销毁时注销事件，防止内存泄漏
        GameEvents.OnBuyCardReq -= HandleBuyCardRequest;
        GameEvents.OnTakeTokensReq -= HandleTakeTokens;
    }

    private void Update()
    {
        bool dataChanged = false;

        // 键盘 1-5 分别增加 白、蓝、绿、红、黑 代币 (模拟作弊获取并扣掉银行的量)
        if (Input.GetKeyDown(KeyCode.Alpha1) && bankTokens[0] > 0) { playerTokens[0]++; bankTokens[0]--; dataChanged = true; }
        if (Input.GetKeyDown(KeyCode.Alpha2) && bankTokens[1] > 0) { playerTokens[1]++; bankTokens[1]--; dataChanged = true; }
        if (Input.GetKeyDown(KeyCode.Alpha3) && bankTokens[2] > 0) { playerTokens[2]++; bankTokens[2]--; dataChanged = true; }
        if (Input.GetKeyDown(KeyCode.Alpha4) && bankTokens[3] > 0) { playerTokens[3]++; bankTokens[3]--; dataChanged = true; }
        if (Input.GetKeyDown(KeyCode.Alpha5) && bankTokens[4] > 0) { playerTokens[4]++; bankTokens[4]--; dataChanged = true; }
        
        // 键盘 G 增加黄金
        if (Input.GetKeyDown(KeyCode.G) && bankGold > 0) { playerGold++; bankGold--; dataChanged = true; }
        
        // 键盘 C 清空资产，并退还给银行
        if (Input.GetKeyDown(KeyCode.C))
        {
            for(int i = 0; i < 5; i++)
            {
                bankTokens[i] += playerTokens[i];
                playerTokens[i] = 0;
                playerDiscounts[i] = 0;
            }
            bankGold += playerGold;
            playerGold = 0;
            playerScore = 0;
            dataChanged = true;
        }

        if (dataChanged)
        {
            PrintCurrentAssets();
            RefreshMarket();
            RefreshPlayerPanel();
            RefreshBankPanel();
        }
    }

    private void RefreshMarket()
    {
        // 调用 MarketManager 的接口刷新排版与交互状态
        marketManager.UpdateMarket(activeCards);
        marketManager.SetMarketInteractable(playerTokens, playerDiscounts, playerGold);
    }

    private void HandleTakeTokens(int[] tokens)
    {
        Debug.Log("[UI事件拦截] 玩家确认拿取代币: 白:" + tokens[0] + " 蓝:" + tokens[1] + " 绿:" + tokens[2] + " 红:" + tokens[3] + " 黑:" + tokens[4]);

        // 再做一次合法性校验（这是服务器本该做的事）
        if (!GameRules.IsValidTokenDraft(tokens, bankTokens))
        {
            Debug.LogWarning("[系统模拟] 拿取代币请求不合法！可能是同色时库存不足或者总数作弊。");
            return;
        }

        // 进行扣费和装兜处理
        for (int i = 0; i < 5; i++)
        {
            bankTokens[i] -= tokens[i];
            playerTokens[i] += tokens[i];
        }

        // 交易完成后再次刷新 UI 与 输出
        PrintCurrentAssets();
        RefreshMarket();
        RefreshPlayerPanel();
        RefreshBankPanel();
    }

    private void HandleBuyCardRequest(int cardId)
    {
        Debug.Log($"[UI事件拦截] 玩家点击购买，目标卡牌 ID: {cardId}");

        // 查找对应的卡牌
        CardSO targetCard = activeCards.Find(c => c.id == cardId);
        if (targetCard != null)
        {
            // 打包该卡牌的花费
            int[] cardCosts = new int[] 
            { 
                targetCard.costWhite, targetCard.costBlue, 
                targetCard.costGreen, targetCard.costRed, targetCard.costBlack 
            };

            // 【双重校验】模拟服务器扣除代币逻辑
            if (GameRules.CanAffordCard(playerTokens, playerDiscounts, playerGold, cardCosts, out int goldNeeded))
            {
                // 扣除相应代币并将其【退还给银行区】
                for (int i = 0; i < 5; i++)
                {
                    int actualCost = Mathf.Max(0, cardCosts[i] - playerDiscounts[i]);
                    int tokenCost = Mathf.Min(actualCost, playerTokens[i]); // 优先扣除普通代币
                    
                    playerTokens[i] -= tokenCost;
                    bankTokens[i] += tokenCost; // 【系统模拟】花出去的钱回到公共货币池
                }
                
                // 扣除黄金并退还
                playerGold -= goldNeeded;
                bankGold += goldNeeded;
                
                // 增加玩家的永久折扣 (假设黄金不会作为折扣产出)
                if ((int)targetCard.bonusGem < 5)
                {
                    playerDiscounts[(int)targetCard.bonusGem]++;
                }
                
                // 增加玩家分数
                playerScore += targetCard.points;

                Debug.Log($"[系统模拟] 购买成功！获得了卡牌 {cardId}，永久折扣+1, 威望分+{targetCard.points}");
            }
            else
            {
                Debug.LogWarning("[系统模拟] 购买失败！代币不足。");
                return;
            }
        }

        // 模拟服务器逻辑：将被购买的卡牌移出市场
        int index = activeCards.FindIndex(c => c.id == cardId);
        if (index != -1)
        {
            activeCards.RemoveAt(index);
            
            // 如果牌库还有剩余，随机补充一张新卡
            if (testDeck.Count > 12)
            {
                CardSO newCard = testDeck[Random.Range(12, testDeck.Count)];
                activeCards.Insert(index, newCard);
                Debug.Log($"[系统模拟] 补充新卡牌 ID: {newCard.id}");
            }
        }

        // 交易完成后再次刷新 UI 与 输出
        PrintCurrentAssets();
        RefreshMarket();
        RefreshPlayerPanel();
        RefreshBankPanel();
    }

    private void RefreshPlayerPanel()
    {
        if (playerPanel != null)
        {
            playerPanel.UpdatePlayerUI(playerTokens, playerDiscounts, playerGold, playerScore);
        }
    }

    private void RefreshBankPanel()
    {
        if (bankUI != null)
        {
            bankUI.UpdateBank(bankTokens, bankGold);
        }
    }

    private void PrintCurrentAssets()
    {
        Debug.Log($"当前模拟资产 -> 白:{playerTokens[0]} 蓝:{playerTokens[1]} 绿:{playerTokens[2]} 红:{playerTokens[3]} 黑:{playerTokens[4]} 金:{playerGold} | 当前折扣总数: {GetTotalDiscounts()} | 分数: {playerScore}");
    }

    private int GetTotalDiscounts()
    {
        int sum = 0;
        foreach (var d in playerDiscounts) sum += d;
        return sum;
    }
}
