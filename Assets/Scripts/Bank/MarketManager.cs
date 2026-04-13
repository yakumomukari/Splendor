using Unity.Netcode;
using UnityEngine;

public class MarketManager : NetworkBehaviour
{
    public static MarketManager Instance { get; private set; }

    // 使用 NetworkList 来同步桌面上翻开的 12 张牌的 ID
    // 假设 0-3 是第一级，4-7 是第二级，8-11 是第三级
    public NetworkList<int> ActiveCardsOnTable;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // NetworkList 必须在 Awake 里初始化
        ActiveCardsOnTable = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 服务器启动时，先往桌面上随便发 12 张牌的 ID（这里用假数据 101, 102 测试）
            // 之后这里要对接你们真正的洗牌逻辑
            for (int i = 0; i < 12; i++)
            {
                ActiveCardsOnTable.Add(100 + i); 
            }
        }
    }

    // 预留给 PlayerNetworkBridge 调用的买卡接口
    [ServerRpc(RequireOwnership = false)]
    public void RequestBuyCardServerRpc(int cardId, ServerRpcParams rpcParams = default)
    {
        // 1. 找到是哪个玩家要买
        // 2. 调用 B 写好的计算器：算算这个玩家的 PlayerInventory 里的钱够不够
        // 3. 够的话：扣钱、加分、把这张卡从 ActiveCardsOnTable 里删掉，抽一张新牌补上
    }
}