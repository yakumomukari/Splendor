using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(NetworkObject))]
public class BankManager : NetworkBehaviour
{
    public static BankManager Instance { get; private set; }

    public NetworkVariable<int> EmeraldCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> SapphireCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> RubyCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> DiamondCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> OnyxCount = new NetworkVariable<int>(7);
    public NetworkVariable<int> GoldCount = new NetworkVariable<int>(5);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Bank] OnNetworkSpawn | IsServer={IsServer} IsClient={IsClient}");

        if (!IsServer) return;

        int playerCount = NetworkManager.Singleton != null
            ? Mathf.Max(1, NetworkManager.Singleton.ConnectedClientsList.Count)
            : 1;

        InitializeBankByPlayerCount(playerCount);
    }

    public void InitializeBankByPlayerCount(int playerCount)
    {
        int baseGemCount = GetBaseGemCountByPlayerCount(playerCount);

        EmeraldCount.Value = baseGemCount;
        SapphireCount.Value = baseGemCount;
        RubyCount.Value = baseGemCount;
        DiamondCount.Value = baseGemCount;
        OnyxCount.Value = baseGemCount;
        GoldCount.Value = 5;

        Debug.Log($"[Bank] 按人数初始化完成。玩家数:{playerCount} 基础宝石:{baseGemCount} 黄金:5");
    }

    private static int GetBaseGemCountByPlayerCount(int playerCount)
    {
        if (playerCount <= 2) return 4;
        if (playerCount == 3) return 5;
        return 7;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTakeTokensServerRpc(int em, int sa, int ru, int di, int on, ServerRpcParams rpcParams = default)
    {
        if (TurnManager.Instance.IsWaitingForReturn.Value) return; // 催债期间，全场静止！
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderClientId)
        {
            GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "不是你的回合你拿个锤子钱！");
            return;
        }
        int totalTaken = em + sa + ru + di + on;

        if (totalTaken > 3 || totalTaken <= 0)
        {
            GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "非法拿取数量！");
            return;
        }

        if (totalTaken == 2)
        {
            bool isValidDouble = (em == 2 && EmeraldCount.Value >= 4) ||
                                 (sa == 2 && SapphireCount.Value >= 4) ||
                                 (ru == 2 && RubyCount.Value >= 4) ||
                                 (di == 2 && DiamondCount.Value >= 4) ||
                                 (on == 2 && OnyxCount.Value >= 4);
            if (!isValidDouble)
            {
                GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "同色拿2个要求银行库存至少为4！");
                return;
            }
        }
        else if (em > 1 || sa > 1 || ru > 1 || di > 1 || on > 1)
        {
            GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "拿3个时必须是不同颜色！");
            return;
        }

        // --- A. 校验逻辑 ---
        if (EmeraldCount.Value >= em && SapphireCount.Value >= sa &&
            RubyCount.Value >= ru && DiamondCount.Value >= di && OnyxCount.Value >= on)
        {
            // --- B. 扣除逻辑 (银行出账) ---
            EmeraldCount.Value -= em;
            SapphireCount.Value -= sa;
            RubyCount.Value -= ru;
            DiamondCount.Value -= di;
            OnyxCount.Value -= on;

            // --- C. 解耦转账 (全服广播) ---
            Debug.Log($"服务器：批准了玩家 {senderClientId} 的拿取代币请求，发起入账广播！");

            int[] takenTokens = new int[] { di, sa, em, ru, on };
            // 银行只负责广播，谁爱听谁听
            GameEvents.OnServerTokensTaken?.Invoke(senderClientId, takenTokens);
        }
        else
        {
            // --- D. 解耦驳回 (全服广播警告) ---
            Debug.LogWarning($"服务器：驳回了玩家 {senderClientId} 的请求，发起警告广播！");

            // 银行只负责广播警告，由负责该玩家的桥接器自己去发 ClientRpc
            GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "银行余额不足，拿取失败！");
        }
    }
    // 供 Player 买卡结算时直接调用的入账接口 (仅限 Server 端运行)
    public void DepositTokens(int[] baseGems, int gold)
    {
        if (!IsServer) return;

        // 假设 baseGems 顺序已经是 [白, 蓝, 绿, 红, 黑]
        DiamondCount.Value += baseGems[0];
        SapphireCount.Value += baseGems[1];
        EmeraldCount.Value += baseGems[2];
        RubyCount.Value += baseGems[3];
        OnyxCount.Value += baseGems[4];

        GoldCount.Value += gold;

        Debug.Log("[Bank] 收到玩家买卡退还的宝石，银行库存已更新。");
    }

    public bool TryTakeGoldToken()
    {
        if (!IsServer) return false;

        if (GoldCount.Value > 0)
        {
            GoldCount.Value--;
            return true;
        }
        return false;
    }
}