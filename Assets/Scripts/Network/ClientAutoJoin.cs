using Unity.Netcode;
using UnityEngine;

public class ClientAutoJoin : MonoBehaviour
{
    void Start()
    {
        // 只有客户端环境（且还没连接过）才执行
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            Debug.Log("已进入游戏场景，正在自动连接服务器...");
            NetworkManager.Singleton.StartClient();
        }
    }
}