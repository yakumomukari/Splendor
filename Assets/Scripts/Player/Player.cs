using Unity.Netcode;
using UnityEngine;
using System;

// 1. 结构体升级：加上转数组的方法，迎合 B 的胃口
public struct TokenAssets : INetworkSerializable
{
    // ⚠️ 极其重要：必须严格按照 B 定的顺序：白, 蓝, 绿, 红, 黑, (金)
    public int White, Blue, Green, Red, Black, Gold;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref White);
        serializer.SerializeValue(ref Blue);
        serializer.SerializeValue(ref Green);
        serializer.SerializeValue(ref Red);
        serializer.SerializeValue(ref Black);
        serializer.SerializeValue(ref Gold);
    }

    // 适配器魔法：给 B 的算法吐出一个长度为 5 的基础宝石数组
    public int[] ToBaseGemArray()
    {
        return new int[] { White, Blue, Green, Red, Black };
    }
}

// 专门用来存预约卡牌 ID 的定长数据结构
public struct ReservedCards : INetworkSerializable
{
    // 默认全填 -1，代表空槽位
    public int Slot1, Slot2, Slot3;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Slot1);
        serializer.SerializeValue(ref Slot2);
        serializer.SerializeValue(ref Slot3);
    }

    // 辅助方法：找空槽塞入卡牌
    public bool TryAddCard(int cardId)
    {
        if (Slot1 == -1) { Slot1 = cardId; return true; }
        if (Slot2 == -1) { Slot2 = cardId; return true; }
        if (Slot3 == -1) { Slot3 = cardId; return true; }
        return false; // 满了，加不进去
    }

    // 辅助方法：买下预约卡后，清空对应槽位
    public void RemoveCard(int cardId)
    {
        if (Slot1 == cardId) Slot1 = -1;
        else if (Slot2 == cardId) Slot2 = -1;
        else if (Slot3 == cardId) Slot3 = -1;
    }
}

// 2. 玩家实体升级：补齐折扣字段与网络交互行为
public class Player : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);

    // 玩家手里的实体代币
    public NetworkVariable<TokenAssets> Tokens = new NetworkVariable<TokenAssets>(
        new TokenAssets(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 玩家买卡积累的永久折扣
    public NetworkVariable<TokenAssets> Discounts = new NetworkVariable<TokenAssets>(
        new TokenAssets(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ==========================================
    // 给你自己用的高阶判定方法
    // ==========================================
    public bool CanAfford(int[] cardCosts)
    {
        return GameRules.CanAffordCard(
            Tokens.Value.ToBaseGemArray(),
            Discounts.Value.ToBaseGemArray(),
            Tokens.Value.Gold,
            cardCosts,
            out _
        );
    }

    // ==========================================
    // 网络生命周期与事件绑定 (Client & Server 缝合区)
    // ==========================================
    public override void OnNetworkSpawn()
    {
        // 如果这个实体是本地玩家控制的（客户端视角）
        if (IsOwner)
        {
            // 监听 B 画的 UI 发出的买卡事件
            GameEvents.OnBuyCardReq += HandleBuyCardRequest;
            GameEvents.OnReserveCardReq += HandleReserveCardRequest;
            GameEvents.OnReturnTokensReq += HandleReturnTokens;

            PlayerPanel localUI = FindObjectOfType<PlayerPanel>();
            if (localUI != null)
            {
                Tokens.OnValueChanged += (oldVal, newVal) => localUI.UpdatePlayerUI(newVal.ToBaseGemArray(), Discounts.Value.ToBaseGemArray(), newVal.Gold, Score.Value);
                Discounts.OnValueChanged += (oldVal, newVal) => localUI.UpdatePlayerUI(Tokens.Value.ToBaseGemArray(), newVal.ToBaseGemArray(), Tokens.Value.Gold, Score.Value);
                Score.OnValueChanged += (oldVal, newVal) => localUI.UpdatePlayerUI(Tokens.Value.ToBaseGemArray(), Discounts.Value.ToBaseGemArray(), Tokens.Value.Gold, newVal);
            }
            // 【新增】：主动把自己的指针塞给市场UI
            if (MarketManager.Instance != null)
            {
                MarketManager.Instance.RegisterLocalPlayer(this);
            }
        }

        // 如果这段代码跑在服务器上（Host 视角）
        if (IsServer)
        {
            // 监听 A 的银行发出的全服入账广播
            GameEvents.OnServerTokensTaken += HandleServerTokensTaken;
        }
    }

    public override void OnNetworkDespawn()
    {
        // 经典的 C# 防内存泄漏操作
        if (IsOwner)
        {
            GameEvents.OnBuyCardReq -= HandleBuyCardRequest;
            GameEvents.OnReserveCardReq -= HandleReserveCardRequest;
            GameEvents.OnReturnTokensReq -= HandleReturnTokens;
        }
        if (IsServer) GameEvents.OnServerTokensTaken -= HandleServerTokensTaken;
    }
    private void HandleReturnTokens(int[] tokensToReturn)
    {
        Debug.Log("[Client] 向服务器上交多余的代币...");
        ReturnTokensServerRpc(tokensToReturn);
    }

    // ==========================================
    // 客户端行为：UI -> RPC 呼叫
    // ==========================================
    private void HandleBuyCardRequest(int cardId)
    {
        Debug.Log($"[Client] 拦截到 UI 买卡点击，卡牌ID: {cardId}。呼叫服务器...");
        BuyCardServerRpc(cardId);
    }

    // ==========================================
    // 服务器行为：核心业务逻辑
    // ==========================================

    [ServerRpc(RequireOwnership = true)]
    private void BuyCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        if (TurnManager.Instance.IsWaitingForReturn.Value) return; // 催债期间，全场静止！
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderId) return;

        // 从全局图鉴拿数据
        CardSO card = GlobalCardDatabase.Instance.GetCard(cardId);
        if (card == null) return;

        int[] cardCosts = new int[] { card.costWhite, card.costBlue, card.costGreen, card.costRed, card.costBlack };

        if (CanAfford(cardCosts))
        {
            // 1. 算出具体扣多少钱 (贪心策略)
            GameRules.CanAffordCard(Tokens.Value.ToBaseGemArray(), Discounts.Value.ToBaseGemArray(), Tokens.Value.Gold, cardCosts, out int goldNeeded);

            var currentTokens = Tokens.Value;
            int[] actualPaid = new int[5];
            int[] playerGems = currentTokens.ToBaseGemArray();
            int[] playerDiscounts = Discounts.Value.ToBaseGemArray();

            for (int i = 0; i < 5; i++)
            {
                int actualCost = Math.Max(0, cardCosts[i] - playerDiscounts[i]);
                actualPaid[i] = Math.Min(actualCost, playerGems[i]);
            }

            // 2. 扣除个人资产
            currentTokens.White -= actualPaid[0];
            currentTokens.Blue -= actualPaid[1];
            currentTokens.Green -= actualPaid[2];
            currentTokens.Red -= actualPaid[3];
            currentTokens.Black -= actualPaid[4];
            currentTokens.Gold -= goldNeeded;
            Tokens.Value = currentTokens;

            // 3. 增加收益 (分数与折扣)
            Score.Value += card.points;
            var currentDiscounts = Discounts.Value;
            switch (card.bonusGem)
            {
                case GemType.White: currentDiscounts.White++; break;
                case GemType.Blue: currentDiscounts.Blue++; break;
                case GemType.Green: currentDiscounts.Green++; break;
                case GemType.Red: currentDiscounts.Red++; break;
                case GemType.Black: currentDiscounts.Black++; break;
            }
            Discounts.Value = currentDiscounts;

            // 4. 银行入账
            BankManager.Instance.DepositTokens(actualPaid, goldNeeded);

            // 5. 【核心兼容】如果买的是预约的卡，从兜里移除；否则从市场移除
            var currentReserved = Reserved.Value;
            if (currentReserved.Slot1 == cardId || currentReserved.Slot2 == cardId || currentReserved.Slot3 == cardId)
            {
                currentReserved.RemoveCard(cardId);
                Reserved.Value = currentReserved;
            }
            else
            {
                GameEvents.OnServerCardBought?.Invoke(cardId);
            }

            // 6. 推进状态机
            if (Score.Value >= 15) Debug.Log("达成胜利条件！");
            TurnManager.Instance.GoToNextTurn();
        }
    }

    // 2. 接收银行的发钱广播
    private void HandleServerTokensTaken(ulong playerId, int[] tokens)
    {
        // 银行是全服广播，咱们只收属于自己的钱
        if (playerId != OwnerClientId) return;

        var current = Tokens.Value;
        current.White += tokens[0];
        current.Blue += tokens[1];
        current.Green += tokens[2];
        current.Red += tokens[3];
        current.Black += tokens[4];

        Tokens.Value = current; // 赋值触发 NetworkVariable 同步

        Debug.Log($"[Player] 银行发钱啦！玩家 {OwnerClientId} 资产已更新，回合结束。");

        // 拿钱动作完成，强制推进状态机
        // TurnManager.Instance.GoToNextTurn();
        TryEndTurn();
    }
    // 新增：玩家捏在手里的预约卡牌
    public NetworkVariable<ReservedCards> Reserved = new NetworkVariable<ReservedCards>(
        new ReservedCards { Slot1 = -1, Slot2 = -1, Slot3 = -1 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private void HandleReserveCardRequest(int cardId)
    {
        Debug.Log($"[Client] 拦截到 UI 预约点击，卡牌ID: {cardId}。呼叫服务器...");
        ReserveCardServerRpc(cardId);
    }

    [ServerRpc(RequireOwnership = true)]
    private void ReserveCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        if (TurnManager.Instance.IsWaitingForReturn.Value) return; // 催债期间，全场静止！
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderId) return;

        var currentReserved = Reserved.Value;
        if (currentReserved.TryAddCard(cardId))
        {
            Reserved.Value = currentReserved;

            // 银行拿黄金
            if (BankManager.Instance.TryTakeGoldToken())
            {
                var t = Tokens.Value;
                t.Gold++;
                Tokens.Value = t;
            }

            // 通知市场销毁这张卡
            GameEvents.OnServerCardBought?.Invoke(cardId);
            // TurnManager.Instance.GoToNextTurn();
            TryEndTurn();
        }
        else
        {
            GameEvents.OnShowWarningMsg?.Invoke("预约位已满！");
        }
    }
    // ==========================================
    // 回合结束校验器 (Server-Side)
    // ==========================================
    private void TryEndTurn()
    {
        if (!IsServer) return;

        var t = Tokens.Value;
        int totalTokens = t.White + t.Blue + t.Green + t.Red + t.Black + t.Gold;

        if (totalTokens > 10)
        {

            int overCount = totalTokens - 10;
            Debug.Log($"[Server] 拦截！玩家 {OwnerClientId} 贪得无厌，代币数量 {totalTokens}/10。强制其归还 {overCount} 个。");
            // 上锁！全局禁止任何操作！
            TurnManager.Instance.IsWaitingForReturn.Value = true;
            RequireReturnTokensClientRpc(overCount);

            // 呼叫客户端弹窗。不用搞复杂的 TargetRpc，直接群发然后靠 IsOwner 本地拦截最稳
            RequireReturnTokensClientRpc(overCount);
        }
        else
        {
            // 没超载，安全放行
            TurnManager.Instance.GoToNextTurn();
        }
    }
    // ==========================================
    // 催债下发 (Server -> Client)
    // ==========================================
    [ClientRpc]
    private void RequireReturnTokensClientRpc(int overCount)
    {
        // if (TurnManager.Instance.IsWaitingForReturn.Value) return; // 催债期间，全场静止！
        // 铁律：只有闯祸的那个玩家的本地替身，才会触发本地 UI 弹窗
        if (!IsOwner) return;

        Debug.Log($"[Client] 收到服务器催债通知：我得还 {overCount} 个代币。");
        // 呼叫 B 画的 UI 弹出一个强制还钱的面板
        GameEvents.OnClientMustReturnTokens?.Invoke(overCount);
    }
    // ==========================================
    // 玩家还钱接收 (Client -> Server)
    // ==========================================
    [ServerRpc(RequireOwnership = true)]
    private void ReturnTokensServerRpc(int[] returnedTokens, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderId) return;

        // 1. 扣除玩家上交的钱
        var current = Tokens.Value;
        current.White -= returnedTokens[0];
        current.Blue -= returnedTokens[1];
        current.Green -= returnedTokens[2];
        current.Red -= returnedTokens[3];
        current.Black -= returnedTokens[4];
        // 如果你们允许还黄金，这里还得加上 current.Gold -= returnedTokens[5]; 
        Tokens.Value = current;

        // 2. 把钱打回银行
        BankManager.Instance.DepositTokens(returnedTokens, 0 /* 黄金数量，视你们规则而定 */);

        Debug.Log($"[Server] 玩家 {senderId} 退还代币完毕，放行回合！");

        // 3. 债务结清，强行推进状态机
        // 解锁，放行！
        TurnManager.Instance.IsWaitingForReturn.Value = false;
        TurnManager.Instance.GoToNextTurn();
    }
}