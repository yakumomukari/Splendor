using Unity.Netcode;
using UnityEngine;

// 这个脚本挂在你们的 Player_Prefab 上
public class PlayerNetworkBridge : NetworkBehaviour
{
    // 当玩家角色在网络中生成时调用
    public override void OnNetworkSpawn()
    {
        // 极其重要的一步：防多端重发判断！
        // 如果这个角色是我自己控制的本地角色，我才去监听 UI 事件
        if (IsOwner)
        {
            // 监听本地 UI 发出的请求，转交给自己的发送函数
            GameEvents.OnTakeTokensReq += HandleTakeTokensRequest;
            GameEvents.OnBuyCardReq += HandleBuyCardRequest;
        }
    }

    // 当玩家断开连接或物体被销毁时，必须注销事件，防止内存泄漏！
    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            GameEvents.OnTakeTokensReq -= HandleTakeTokensRequest;
            GameEvents.OnBuyCardReq -= HandleBuyCardRequest;
        }
    }

    // ==========================================
    // 发送端：处理“拿取代币”事件 (发给服务器)
    // ==========================================
    private void HandleTakeTokensRequest(int[] tokensReq)
    {
        // 检查传进来的数组长度是否合法 (5种宝石 + 1种黄金)
        if (tokensReq == null || tokensReq.Length < 5) return;

        // 调用 BankManager 的 ServerRpc，把请求发给服务器权威端
        BankManager.Instance.RequestTakeTokensServerRpc(
            tokensReq[0], // 绿
            tokensReq[1], // 蓝
            tokensReq[2], // 红
            tokensReq[3], // 钻
            tokensReq[4]  // 黑
        );
    }

    private void HandleBuyCardRequest(int cardId)
    {
        // 向服务器发送买卡请求 (待后续完善)
    }

    // ==========================================
    // 接收端：接收服务器的精准警告 (服务器发给本地)
    // ==========================================
    [ClientRpc]
    public void SendWarningToClientRpc(string msg, ClientRpcParams clientRpcParams = default)
    {
        // 只有被服务器 TargetClientIds 点名的那个客户端，才会执行到这里！
        Debug.Log($"[本地客户端收到警告] {msg}");
        
        // 触发本地的 UI 弹窗 Action，让 UI 同学的界面弹出来
        GameEvents.OnShowWarningMsg?.Invoke(msg);
    }
}