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
        if (TurnManager.Instance.IsWaitingForReturn.Value) return;
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (TurnManager.Instance.CurrentActivePlayerId.Value != senderClientId)
        {
            GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "不是你的回合你拿个锤子钱！");
            return;
        }

        // --- A. 核心规则校验 ---
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(senderClientId, out var client))
        {
            Player p = client.PlayerObject.GetComponent<Player>();
            if (p != null)
            {
                var t = p.Tokens.Value;
                int currentTotal = t.White + t.Blue + t.Green + t.Red + t.Black + t.Gold;

                int[] selectedTokens = new int[] { em, sa, ru, di, on };
                int[] bankRemaining = new int[] { EmeraldCount.Value, SapphireCount.Value, RubyCount.Value, DiamondCount.Value, OnyxCount.Value };

                if (!GameRules.IsValidTokenDraft(selectedTokens, bankRemaining, currentTotal))
                {
                    // 防止有黑客跳过客户端直接发 RPC，服务端做最后一道铁壁
                    GameEvents.OnServerTakeTokensFailed?.Invoke(senderClientId, "非法的拿取数量或组合！");
                    return;
                }

                // --- B. 扣除逻辑 (银行出账) ---
                EmeraldCount.Value -= em;
                SapphireCount.Value -= sa;
                RubyCount.Value -= ru;
                DiamondCount.Value -= di;
                OnyxCount.Value -= on;

                // --- C. 解耦转账 (全服广播) ---
                Debug.Log($"[Bank-Server] 批准了玩家 {senderClientId} 的拿取代币请求。");

                int[] takenTokens = new int[] { di, sa, em, ru, on };
                GameEvents.OnServerTokensTaken?.Invoke(senderClientId, takenTokens);
            }
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