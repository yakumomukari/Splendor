using Unity.Netcode;
using UnityEngine;
using System;

// 1. 结构体定义
[Serializable]
public struct TokenAssets : INetworkSerializable
{
    // 必须严格按照顺序：白, 蓝, 绿, 红, 黑, 金
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

    public int[] ToBaseGemArray()
    {
        return new int[] { White, Blue, Green, Red, Black };
    }
}

[Serializable]
public struct ReservedCards : INetworkSerializable
{
    public int Slot1, Slot2, Slot3;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Slot1);
        serializer.SerializeValue(ref Slot2);
        serializer.SerializeValue(ref Slot3);
    }

    public bool TryAddCard(int cardId)
    {
        if (Slot1 == -1) { Slot1 = cardId; return true; }
        if (Slot2 == -1) { Slot2 = cardId; return true; }
        if (Slot3 == -1) { Slot3 = cardId; return true; }
        return false;
    }

    public void RemoveCard(int cardId)
    {
        if (Slot1 == cardId) Slot1 = -1;
        else if (Slot2 == cardId) Slot2 = -1;
        else if (Slot3 == cardId) Slot3 = -1;
    }
}

// 2. 玩家实体类
public class Player : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);

    public NetworkVariable<TokenAssets> Tokens = new NetworkVariable<TokenAssets>(
        new TokenAssets(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<TokenAssets> Discounts = new NetworkVariable<TokenAssets>(
        new TokenAssets(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ReservedCards> Reserved = new NetworkVariable<ReservedCards>(
        new ReservedCards { Slot1 = -1, Slot2 = -1, Slot3 = -1 },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ==========================================
    // 逻辑判定
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
    // 生命周期
    // ==========================================
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            GameEvents.OnTakeTokensReq += HandleTakeTokensRequest;
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

            if (MarketManager.Instance != null) MarketManager.Instance.RegisterLocalPlayer(this);
        }

        if (IsServer)
        {
            GameEvents.OnServerTokensTaken += HandleServerTokensTaken;
            GameEvents.OnServerTakeTokensFailed += HandleServerTakeTokensFailed;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            GameEvents.OnTakeTokensReq -= HandleTakeTokensRequest;
            GameEvents.OnBuyCardReq -= HandleBuyCardRequest;
            GameEvents.OnReserveCardReq -= HandleReserveCardRequest;
            GameEvents.OnReturnTokensReq -= HandleReturnTokens;
        }
        if (IsServer)
        {
            GameEvents.OnServerTokensTaken -= HandleServerTokensTaken;
            GameEvents.OnServerTakeTokensFailed -= HandleServerTakeTokensFailed;
        }
    }

    // ==========================================
    // 客户端行为处理
    // ==========================================
    private void HandleTakeTokensRequest(int[] requestedTokens)
    {
        if (requestedTokens == null || requestedTokens.Length < 5) return;
        if (BankManager.Instance == null) return;

        // UI顺序转银行顺序并发送请求
        BankManager.Instance.RequestTakeTokensServerRpc(requestedTokens[2], requestedTokens[1], requestedTokens[3], requestedTokens[0], requestedTokens[4]);
    }

    private void HandleBuyCardRequest(int cardId) => BuyCardServerRpc(cardId);
    private void HandleReserveCardRequest(int cardId) => ReserveCardServerRpc(cardId);
    private void HandleReturnTokens(int[] tokensToReturn) => ReturnTokensServerRpc(tokensToReturn);

    // ==========================================
    // 服务器核心逻辑 (RPCs)
    // ==========================================

    [ServerRpc(RequireOwnership = true)]
    private void BuyCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        // 基础验证
        if (TurnManager.Instance == null || TurnManager.Instance.IsWaitingForReturn.Value)
        {
            ShowBuyFailedClientRpc("系统忙或正在归还代币。", BuildSingleTargetRpcParams(senderId));
            return;
        }

        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderId)
        {
            ShowBuyFailedClientRpc("还没轮到你行动。", BuildSingleTargetRpcParams(senderId));
            return;
        }

        if (GlobalCardDatabase.Instance == null) return;

        CardSO card = GlobalCardDatabase.Instance.GetCard(cardId);
        if (card == null) return;

        // 区域检查：是否在预约位或市场可见
        bool isReserved = Reserved.Value.Slot1 == cardId || Reserved.Value.Slot2 == cardId || Reserved.Value.Slot3 == cardId;
        bool isVisible = MarketDeckManager.Instance != null && MarketDeckManager.Instance.IsCardVisible(cardId);
        
        if (!isReserved && !isVisible)
        {
            ShowBuyFailedClientRpc("卡牌已不在购买区。", BuildSingleTargetRpcParams(senderId));
            return;
        }

        int[] cardCosts = new int[] { card.costWhite, card.costBlue, card.costGreen, card.costRed, card.costBlack };

        if (!CanAfford(cardCosts))
        {
            ShowBuyFailedClientRpc("代币不足。", BuildSingleTargetRpcParams(senderId));
            return;
        }

        // --- 开始结算 ---
        GameRules.CanAffordCard(Tokens.Value.ToBaseGemArray(), Discounts.Value.ToBaseGemArray(), Tokens.Value.Gold, cardCosts, out int goldNeeded);

        var currentTokens = Tokens.Value;
        int[] playerGems = currentTokens.ToBaseGemArray();
        int[] playerDiscounts = Discounts.Value.ToBaseGemArray();
        int[] actualPaid = new int[5];

        for (int i = 0; i < 5; i++)
        {
            int actualCost = Math.Max(0, cardCosts[i] - playerDiscounts[i]);
            actualPaid[i] = Math.Min(actualCost, playerGems[i]);
        }

        // 1. 扣除代币
        currentTokens.White -= actualPaid[0];
        currentTokens.Blue -= actualPaid[1];
        currentTokens.Green -= actualPaid[2];
        currentTokens.Red -= actualPaid[3];
        currentTokens.Black -= actualPaid[4];
        currentTokens.Gold -= goldNeeded;
        Tokens.Value = currentTokens;

        // 2. 增加分数与折扣
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

        // 3. 银行入账
        BankManager.Instance.DepositTokens(actualPaid, goldNeeded);

        // 4. 判定贵族
        if (NobleManager.Instance != null) NobleManager.Instance.TryGrantNobleToPlayer(this);

        // 5. 移除卡牌源
        if (isReserved)
        {
            var res = Reserved.Value;
            res.RemoveCard(cardId);
            Reserved.Value = res;
        }
        else
        {
            GameEvents.OnServerCardBought?.Invoke(cardId);
        }

        // 6. 终局判定与回合推进
        if (Score.Value >= 15 && !TurnManager.Instance.IsLastRound.Value)
        {
            Debug.Log($"[Server] 玩家 {senderId} 达到 15 分，触发终局圈！");
            TurnManager.Instance.IsLastRound.Value = true;
        }
        TryEndTurn();
    }

    [ServerRpc(RequireOwnership = true)]
    private void ReserveCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (TurnManager.Instance.IsWaitingForReturn.Value || TurnManager.Instance.CurrentActivePlayerId.Value != senderId) return;

        var currentReserved = Reserved.Value;
        if (currentReserved.TryAddCard(cardId))
        {
            Reserved.Value = currentReserved;
            if (BankManager.Instance.TryTakeGoldToken())
            {
                var t = Tokens.Value;
                t.Gold++;
                Tokens.Value = t;
            }
            GameEvents.OnServerCardBought?.Invoke(cardId);
            TryEndTurn();
        }
        else
        {
            GameEvents.OnShowWarningMsg?.Invoke("预约位已满！");
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void ReturnTokensServerRpc(int[] returnedTokens, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderId) return;

        var current = Tokens.Value;
        current.White -= returnedTokens[0];
        current.Blue -= returnedTokens[1];
        current.Green -= returnedTokens[2];
        current.Red -= returnedTokens[3];
        current.Black -= returnedTokens[4];
        Tokens.Value = current;

        BankManager.Instance.DepositTokens(returnedTokens, 0);
        TurnManager.Instance.IsWaitingForReturn.Value = false;
        TurnManager.Instance.GoToNextTurn();
    }

    private void TryEndTurn()
    {
        if (!IsServer) return;
        var t = Tokens.Value;
        int total = t.White + t.Blue + t.Green + t.Red + t.Black + t.Gold;

        if (total > 10)
        {
            TurnManager.Instance.IsWaitingForReturn.Value = true;
            RequireReturnTokensClientRpc(total - 10);
        }
        else
        {
            TurnManager.Instance.GoToNextTurn();
        }
    }

    // ==========================================
    // 事件监听与回调
    // ==========================================
    private void HandleServerTokensTaken(ulong playerId, int[] tokens)
    {
        if (playerId != OwnerClientId) return;
        var current = Tokens.Value;
        current.White += tokens[0];
        current.Blue += tokens[1];
        current.Green += tokens[2];
        current.Red += tokens[3];
        current.Black += tokens[4];
        Tokens.Value = current;
        TryEndTurn();
    }

    private void HandleServerTakeTokensFailed(ulong playerId, string reason)
    {
        if (playerId == OwnerClientId) ShowTakeTokenFailedClientRpc(reason);
    }

    // ==========================================
    // ClientRpcs
    // ==========================================
    [ClientRpc]
    private void RequireReturnTokensClientRpc(int overCount)
    {
        if (IsOwner) GameEvents.OnClientMustReturnTokens?.Invoke(overCount);
    }

    [ClientRpc]
    private void ShowBuyFailedClientRpc(string reason, ClientRpcParams params_ = default)
    {
        if (IsOwner) GameEvents.OnShowWarningMsg?.Invoke(reason);
    }

    [ClientRpc]
    private void ShowTakeTokenFailedClientRpc(string reason)
    {
        if (IsOwner) GameEvents.OnShowWarningMsg?.Invoke(reason);
    }

    private static ClientRpcParams BuildSingleTargetRpcParams(ulong clientId)
    {
        return new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } } };
    }
}