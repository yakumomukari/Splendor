using Unity.Netcode;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    void Start()
    {
        // #if UNITY_SERVER 这是一个宏定义
        // 它的意思是：这段代码只有在你勾选了 "Dedicated Server" 打包出来的 Linux 程序里才会执行
        // 在你本地电脑上平时点 Play 是绝对不会执行的，非常安全！
        #if UNITY_SERVER
        Debug.Log("====================================");
        Debug.Log("开始自动启动 WebSocket 服务器...");
        Debug.Log("监听端口应为 Inspector 面板中设置的 8080");
        Debug.Log("====================================");
        
        // 强制启动服务端
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ServerAutoStart] NetworkManager.Singleton 为空，无法启动服务器。");
            return;
        }

        bool ok = NetworkManager.Singleton.StartServer();
        Debug.Log(ok
            ? "[ServerAutoStart] StartServer 成功。"
            : "[ServerAutoStart] StartServer 失败，请检查传输层与端口配置。");

        // 延迟一帧后检查关键系统是否存在，快速定位场景里缺脚本/缺组件问题。
        Invoke(nameof(ValidateNetworkSceneState), 0.5f);
        #endif
    }

    private void ValidateNetworkSceneState()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ServerAutoStart] 校验失败：NetworkManager.Singleton 为空。");
            return;
        }

        GameObject netObj = NetworkManager.Singleton.gameObject;
        if (netObj != null)
        {
            Component[] components = netObj.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogError("[ServerAutoStart] 检测到 NetworkObject 上存在 Missing Script，请在编辑器中移除并重新挂载正确脚本。");
                    break;
                }
            }
        }

        Debug.Log($"[ServerAutoStart] 系统探针: BankManager={(BankManager.Instance != null)}, TurnManager={(TurnManager.Instance != null)}, MarketDeckManager={(MarketDeckManager.Instance != null)}, NobleManager={(NobleManager.Instance != null)}");
    }
}