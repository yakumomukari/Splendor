using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
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

    // 新增：防老赖专用锁
    public NetworkVariable<bool> IsWaitingForReturn = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // 【新增】终局标志位
    public NetworkVariable<bool> IsLastRound = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("开发测试：凑齐几个人自动开局？")]
    public int playersNeededToStart = 1; // 测试时设为2，打局时设为4

    private bool gameHasStarted = false;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[TurnManager] OnNetworkSpawn | IsServer={IsServer} IsClient={IsClient}");

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
    // 修改你的 OnClientConnected，加入发令枪逻辑
    private void OnClientConnected(ulong clientId)
    {
        // 如果游戏已经开始了，这人就是中途乱入的观战者，别加进座位表！
        if (gameHasStarted) return;

        if (!playerOrder.Contains(clientId))
        {
            playerOrder.Add(clientId);
            Debug.Log($"[TurnManager] 玩家 {clientId} 加入，当前总人数: {playerOrder.Count}");

            // 凑齐人数，自动发车！
            if (playerOrder.Count >= playersNeededToStart)
            {
                gameHasStarted = true;
                Debug.Log("[TurnManager] 人数凑齐，正式开局！下发 UI 座位表！");

                // 1. 把顺序转成数组发给所有客户端去构建 UI
                InitializeUIClientRpc(playerOrder.ToArray());

                // 2. 顺便让 A 组的银行或者市场发牌机也在这里完成初始化（如果有的话）
            }
        }
    }

    // 有人掉线了，踢出座位表
    private void OnClientDisconnected(ulong clientId)
    {
        int disconnectIndex = playerOrder.IndexOf(clientId);
        if (disconnectIndex == -1) return;
        playerOrder.Remove(clientId);

        // 极限防死锁：如果恰好是当前回合的人掉线了，强制切给下一个人
        if (CurrentActivePlayerId.Value == clientId && playerOrder.Count > 0)
        {
            Debug.LogWarning($"[TurnManager] 当前回合玩家 {clientId} 掉线！强制切回合。");
            // 确保切给原本坐在他后面的那个人
            int nextIndex = disconnectIndex % playerOrder.Count;
            CurrentActivePlayerId.Value = playerOrder[nextIndex];
        }
    }
    // 全服广播：强行唤醒客户端的 UI 管家
    [ClientRpc]
    private void InitializeUIClientRpc(ulong[] orderArray)
    {
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.BuildLayout(new List<ulong>(orderArray));
        }
    }

    // ==========================================
    // 状态流转引擎：由 A (拿钱) 或你自己的 Player (买卡) 在操作成功后调用
    // ==========================================
    public void GoToNextTurn()
    {
        if (!IsServer || playerOrder.Count == 0) return;

        int currentIndex = playerOrder.IndexOf(CurrentActivePlayerId.Value);
        if (currentIndex == -1) currentIndex = 0;

        int nextIndex = (currentIndex + 1) % playerOrder.Count;

        // 【新增】结算判定：如果转回了一号位（房主/建房者），且已经是最后一圈了，游戏彻底结束！
        if (nextIndex == 0 && IsLastRound.Value)
        {
            Debug.Log("[TurnManager] 所有玩家回合数一致，进入清算环节。");
            CalculateWinnerAndEndGame(); // 👈 调这里
            return;
        }

        CurrentActivePlayerId.Value = playerOrder[nextIndex];
        Debug.Log($"[TurnManager] 回合切换！现在是 玩家 {CurrentActivePlayerId.Value} 的回合");
    }
    // ==========================================
    // 终局清算与胜负仲裁 (Task 6)
    // ==========================================
    private void CalculateWinnerAndEndGame()
    {
        ulong winnerId = playerOrder[0];
        int highestScore = -1;
        int lowestCardCount = int.MaxValue; // 用于平局决胜：买的卡越少越牛逼

        // O(N) 遍历所有玩家，找出真正的赢家
        foreach (ulong clientId in playerOrder)
        {
            // 通过 NGO 底层拿到对应客户端的玩家实体
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                Player p = client.PlayerObject.GetComponent<Player>();
                if (p != null)
                {
                    int pScore = p.Score.Value;
                    // 玩家买的卡牌总数 = 五种颜色折扣的总和
                    var d = p.Discounts.Value;
                    int pCardCount = d.White + d.Blue + d.Green + d.Red + d.Black;

                    if (pScore > highestScore)
                    {
                        highestScore = pScore;
                        lowestCardCount = pCardCount;
                        winnerId = clientId;
                    }
                    else if (pScore == highestScore)
                    {
                        // 触发决胜规则：同分比开发卡数量，越少越好
                        if (pCardCount < lowestCardCount)
                        {
                            lowestCardCount = pCardCount;
                            winnerId = clientId;
                        }
                    }
                }
            }
        }

        Debug.Log($"[Server] 游戏彻底结束！最终赢家: 玩家 {winnerId}，分数: {highestScore}，消耗卡牌: {lowestCardCount}");

        // 呼叫全体客户端弹出结算画面
        NotifyGameEndClientRpc(winnerId, highestScore);
    }
    [ClientRpc]
    private void NotifyGameEndClientRpc(ulong winnerId, int winningScore)
    {
        if (winnerId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[Client] 我赢了！");
        }

        // 抛出事件，C 组的 UI 监听这个事件去画终局排行榜
        // 注意：你需要去 GameEvents.cs 里补一句 public static Action<ulong, int> OnGameEnded;
        GameEvents.OnGameEnded?.Invoke(winnerId, winningScore);
    }
}