using Unity.Netcode;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    void Start()
    {
        // 1. 如果是在编辑器里点 Play
        if (Application.isEditor)
        {
            Debug.Log("[ServerAutoStart] 检测到处于编辑器模式，跳过自动启动 Server。");
            // 如果你想让它自动连 Linux，可以写下一行，不想自动连就注释掉
            // NetworkManager.Singleton.StartClient(); 
            return; 
        }

        // 2. 如果是在 Linux 服务器上运行（非编辑器环境）
        #if UNITY_SERVER
        Debug.Log("====================================");
        Debug.Log("检测到专用服务器打包环境，正在启动 WebSocket 服务器...");
        Debug.Log("====================================");

        if (NetworkManager.Singleton != null)
        {
            bool ok = NetworkManager.Singleton.StartServer();
            Debug.Log(ok ? "服务器启动成功！" : "服务器启动失败！");
        }
        #endif
    }
}