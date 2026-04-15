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
    [Tooltip("仅本地联调用：同一进程启动 Host(服务器+客户端)")]
    public bool AutoStartHostOnPlay = false;

    private bool subscribedCallbacks;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkBootstrap] NetworkManager.Singleton 为空，无法启动联机。");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkBootstrap] 未找到 UnityTransport 组件，无法启动联机。");
            return;
        }

        SubscribeConnectionCallbacks();

        if (AutoStartHostOnPlay)
        {
            StartHostForLocalTest(transport);
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
                Invoke(nameof(CheckClientConnectionState), 4f);
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

    private void StartHostForLocalTest(UnityTransport transport)
    {
        transport.SetConnectionData("127.0.0.1", ServerPort);

        bool ok = NetworkManager.Singleton.StartHost();
        if (ok)
        {
            Debug.Log($"[NetworkBootstrap] 本地Host已启动，端口: {ServerPort}");
        }
        else
        {
            Debug.LogError("[NetworkBootstrap] 本地Host启动失败。请检查端口占用与NetworkManager配置。");
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

    private void SubscribeConnectionCallbacks()
    {
        if (subscribedCallbacks || NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        subscribedCallbacks = true;
    }

    private void OnDestroy()
    {
        if (!subscribedCallbacks || NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        subscribedCallbacks = false;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkBootstrap] 客户端连接成功: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[NetworkBootstrap] 客户端断开连接: {clientId}");
    }

    private void CheckClientConnectionState()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient) return;
        if (NetworkManager.Singleton.IsConnectedClient) return;

        Debug.LogError($"[NetworkBootstrap] 客户端尚未连上服务器，请确认服务器是否已启动、IP={ServerIP} 端口={ServerPort} 是否正确。\n如本机单窗口联调，请勾选 AutoStartHostOnPlay。");
    }
}