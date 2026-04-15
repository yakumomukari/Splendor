using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
    // 这里填入你们实验室服务器的真实 IP 地址
    public string ServerIP = "115.25.46.153"; 
    public ushort ServerPort = 7777;
    [Header("联机测试开关")]
    public bool AutoStartClientOnPlay = true;

    private void Start()
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkBootstrap] 未找到 UnityTransport 组件，无法启动联机。");
            return;
        }

        // 关键判断：检查启动时有没有带 "-server" 这个命令行参数
        if (System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-server"))
        {
            StartDedicatedServer(transport);
        }
        else
        {
            ConfigureClientTransport(transport);
            if (AutoStartClientOnPlay)
            {
                StartClient();
            }
            else
            {
                Debug.Log("[NetworkBootstrap] 已配置客户端连接参数，等待手动调用 StartClient().");
            }
        }
    }

    private void StartDedicatedServer(UnityTransport transport)
    {
        // 服务器必须监听 0.0.0.0，才能接收所有网卡的连接
        transport.SetConnectionData("0.0.0.0", ServerPort);

        bool ok = NetworkManager.Singleton.StartServer();
        if (ok)
        {
            Debug.Log($"[NetworkBootstrap] 专用服务器已启动，监听端口: {ServerPort}");
        }
        else
        {
            Debug.LogError("[NetworkBootstrap] 服务器启动失败。请检查端口占用与NetworkManager配置。");
        }
    }

    private void ConfigureClientTransport(UnityTransport transport)
    {
        transport.SetConnectionData(ServerIP, ServerPort);
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkBootstrap] NetworkManager.Singleton 为空，无法启动客户端。");
            return;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[NetworkBootstrap] 当前已在联机会话中，忽略重复 StartClient().");
            return;
        }

        bool ok = NetworkManager.Singleton.StartClient();
        if (ok)
        {
            Debug.Log($"[NetworkBootstrap] 客户端启动成功，正在连接 {ServerIP}:{ServerPort}");
        }
        else
        {
            Debug.LogError("[NetworkBootstrap] 客户端启动失败。请检查IP、端口、传输层配置。");
        }
    }
}