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
        NetworkManager.Singleton.StartServer();
        #endif
    }
}