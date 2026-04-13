using Unity.Netcode;
using UnityEngine;


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

    [ServerRpc(RequireOwnership = false)]
    public void RequestTakeTokensServerRpc(int em, int sa, int ru, int di, int on, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        
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
            
            int[] takenTokens = new int[] { em, sa, ru, di, on };
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
}