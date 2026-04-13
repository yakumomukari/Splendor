using Unity.Netcode;
using UnityEngine;

// 1. 定义宝石类型的枚举，方便大家统一调用
public enum GemType { Emerald, Sapphire, Ruby, Diamond, Onyx, Gold }

public class BankManager : NetworkBehaviour
{
    // 做成单例模式，方便其他脚本和 UI 直接拿到它
    public static BankManager Instance { get; private set; }

    // ==========================================
    // 2. 核心网络变量 (真理数据)
    // 权限设为 Server，意味着只有服务器能修改它们，客户端只能干瞪眼看着
    // ==========================================
    public NetworkVariable<int> EmeraldCount = new NetworkVariable<int>(7); // 绿宝石 (假设3人局初始为7)
    public NetworkVariable<int> SapphireCount = new NetworkVariable<int>(7); // 蓝宝石
    public NetworkVariable<int> RubyCount = new NetworkVariable<int>(7);     // 红宝石
    public NetworkVariable<int> DiamondCount = new NetworkVariable<int>(7);  // 钻石
    public NetworkVariable<int> OnyxCount = new NetworkVariable<int>(7);     // 黑曜石
    public NetworkVariable<int> GoldCount = new NetworkVariable<int>(5);     // 黄金 (固定5个)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ==========================================
    // 3. 供客户端调用的 RPC 接口 (拿钱全靠它)
    // ==========================================
    [ServerRpc(RequireOwnership = false)]
    public void RequestTakeTokensServerRpc(int em, int sa, int ru, int di, int on, ServerRpcParams rpcParams = default)
    {
        // rpcParams 可以帮我们找出是哪个客户端发起的请求
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
        // --- A. 校验逻辑 (防作弊核心) ---
        if (EmeraldCount.Value >= em && SapphireCount.Value >= sa && 
            RubyCount.Value >= ru && DiamondCount.Value >= di && OnyxCount.Value >= on)
        {
            // --- B. 扣除逻辑 (银行出账) ---
            EmeraldCount.Value -= em;
            SapphireCount.Value -= sa;
            RubyCount.Value -= ru;
            DiamondCount.Value -= di;
            OnyxCount.Value -= on;

            // --- C. 转账逻辑 (玩家入账) ---
            NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(senderClientId);
            if (playerObj != null)
            {
                // 获取玩家身上的背包脚本，把钱发给他
                var inventory = playerObj.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    inventory.AddTokensServerSide(em, sa, ru, di, on);
                    Debug.Log($"服务器：批准了玩家 {senderClientId} 的拿取代币请求，并已完成转账！");
                }
            }
        }
        else
        {
            // --- D. 驳回与警告逻辑 (精准回传) ---
            Debug.LogWarning($"服务器：驳回了玩家 {senderClientId} 的请求，银行余额不足！");

            // 1. 在服务器上，找到发送请求的那个玩家的网络本体
            NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(senderClientId);
            
            if (playerObj != null)
            {
                // 2. 获取他身上的桥接脚本
                var bridge = playerObj.GetComponent<PlayerNetworkBridge>();
                if (bridge != null)
                {
                    // 3. 极其重要：设置定向发送参数，只发给犯规的这个人！
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
                    };
                    
                    // 4. 发射 ClientRpc！
                    bridge.SendWarningToClientRpc("银行余额不足，拿取失败！", clientRpcParams);
                }
            }
        }
    }
}