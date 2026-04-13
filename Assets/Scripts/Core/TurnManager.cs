using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class TurnManager : NetworkBehaviour
{
    // 单例，方便 A 的 BankManager 直接读状态拦截非法请求
    public static TurnManager Instance { get; private set; }

    // 核心状态锁：当前是谁的回合
    public NetworkVariable<ulong> CurrentActivePlayerId = new NetworkVariable<ulong>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 【核心】服务器端维护的玩家真实顺序表 (不参与网络序列化，只存在于服务器内存)
    private List<ulong> playerOrder = new List<ulong>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 服务器启动时，订阅连接/断开事件
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // 把 Host (房主，ID为0) 自己先加进座位表
            playerOrder.Add(NetworkManager.ServerClientId);
            CurrentActivePlayerId.Value = NetworkManager.ServerClientId;
            Debug.Log("玩家 0 加入");
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            // 防内存泄漏基操
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // 有人连进来了，拉入座位表
    private void OnClientConnected(ulong clientId)
    {
        if (!playerOrder.Contains(clientId))
        {
            playerOrder.Add(clientId);
            Debug.Log($"[TurnManager] 玩家 {clientId} 加入，当前总人数: {playerOrder.Count}");
        }
    }

    // 有人掉线了，踢出座位表
    private void OnClientDisconnected(ulong clientId)
    {
        playerOrder.Remove(clientId);

        // 极限防死锁：如果恰好是当前回合的人掉线了，强制切给下一个人
        if (CurrentActivePlayerId.Value == clientId && playerOrder.Count > 0)
        {
            Debug.LogWarning($"[TurnManager] 当前回合玩家 {clientId} 掉线！强制切回合。");
            GoToNextTurn();
        }
    }

    // ==========================================
    // 状态流转引擎：由 A (拿钱) 或你自己的 Player (买卡) 在操作成功后调用
    // ==========================================
    public void GoToNextTurn()
    {
        if (!IsServer || playerOrder.Count == 0) return;

        // 找当前人在列表里的真实索引
        int currentIndex = playerOrder.IndexOf(CurrentActivePlayerId.Value);

        // 如果找不到（比如出Bug了），强行归零
        if (currentIndex == -1) currentIndex = 0;

        // 经典数组取模切回合
        int nextIndex = (currentIndex + 1) % playerOrder.Count;

        // 修改 NetworkVariable，NGO 自动广播全场
        CurrentActivePlayerId.Value = playerOrder[nextIndex];

        Debug.Log($"[TurnManager] 回合切换！现在是 玩家 {CurrentActivePlayerId.Value} 的回合");
    }
}