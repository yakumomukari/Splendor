using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
    // 这里填入你们实验室服务器的真实 IP 地址
    public string ServerIP = "115.25.46.153"; 
    public ushort ServerPort = 7777;

    private void Start()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // 关键判断：检查启动时有没有带 "-server" 这个命令行参数
        if (System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-server"))
        {
            // --- 服务器模式 ---
            // 服务器必须监听 0.0.0.0，才能接收所有网卡的连接
            transport.SetConnectionData("0.0.0.0", ServerPort);
            NetworkManager.Singleton.StartServer();
            Debug.Log($"专用服务器已启动，正在监听端口: {ServerPort}");
        }
        else
        {
            // --- 客户端模式 ---
            // 客户端需要去连接服务器的真实 IP
            transport.SetConnectionData(ServerIP, ServerPort);
            // 提示：在测试阶段，你可以写两个 UI 按钮手动调用 StartClient()，而不是一启动就连。
        }
    }
}