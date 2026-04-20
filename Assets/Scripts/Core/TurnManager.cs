using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("游戏状态")]
    public NetworkVariable<ulong> CurrentActivePlayerId = new NetworkVariable<ulong>(
        ulong.MaxValue, // 初始设为无效值，防止未开局就判定为玩家0
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsWaitingForReturn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> IsLastRound = new NetworkVariable<bool>(false);

    // 动态人数配置：由第一个进来的玩家决定
    private int playersNeededToStart = 2; 
    private List<ulong> playerOrder = new List<ulong>();
    private bool gameHasStarted = false;

    // UI 构建缓冲
    private ulong[] pendingUiOrder;
    private bool pendingUiLayout;
    private float pendingUiDeadline;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // 专用服务器初始化：清空状态，等待真实玩家
            playerOrder.Clear();
            CurrentActivePlayerId.Value = ulong.MaxValue;
            gameHasStarted = false;
            Debug.Log("[TurnManager] 服务器就绪，等待玩家连入...");
        }

        // 如果是客户端连入，立刻把菜单里选的人数发给服务器（仅限第一个人）
        if (IsClient && !IsServer)
        {
            SetPlayerCountServerRpc(StaticGameSettings.DesiredPlayerCount);
        }
    }

    // [重要] 服务器根据房主的选择更新开局人数
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerCountServerRpc(int count)
    {
        if (gameHasStarted) return;
        // 只有当第一个玩家连入时，才接受他的人数设置
        if (playerOrder.Count <= 1)
        {
            playersNeededToStart = count;
            Debug.Log($"[TurnManager] 服务器收到人数设定：{playersNeededToStart} 人局");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (gameHasStarted) return;

        if (!playerOrder.Contains(clientId))
        {
            playerOrder.Add(clientId);
            Debug.Log($"[TurnManager] 玩家 {clientId} 入座。当前: {playerOrder.Count}/{playersNeededToStart}");

            // 凑齐人数，发车！
            if (playerOrder.Count >= playersNeededToStart)
            {
                StartGame();
            }
        }
    }

    private void StartGame()
    {
        gameHasStarted = true;
        // 确定第一个行动者：playerOrder里的第一个人
        CurrentActivePlayerId.Value = playerOrder[0];
        
        Debug.Log($"[TurnManager] 游戏正式开始！首回合玩家: {CurrentActivePlayerId.Value}");

        // 通知所有客户端构建 UI 座位表
        BroadcastLayout(playerOrder.ToArray());
        
        // 此处可以触发 MarketDeckManager.Instance.InitializeMarket();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!playerOrder.Contains(clientId)) return;
        
        int disconnectIndex = playerOrder.IndexOf(clientId);
        playerOrder.Remove(clientId);

        if (CurrentActivePlayerId.Value == clientId && playerOrder.Count > 0)
        {
            // 如果掉线的是当前回合玩家，强制切入下一位
            int nextIndex = disconnectIndex % playerOrder.Count;
            CurrentActivePlayerId.Value = playerOrder[nextIndex];
        }
    }

    // --- UI 座位表同步逻辑 ---
    [ClientRpc]
    private void InitializeUIClientRpc(ulong[] orderArray, ClientRpcParams clientRpcParams = default)
    {
        pendingUiOrder = orderArray;
        pendingUiLayout = true;
        pendingUiDeadline = Time.unscaledTime + 10f;
        TryBuildClientLayout();
    }

    private void BroadcastLayout(ulong[] orderArray) => InitializeUIClientRpc(orderArray);

    private void Update()
    {
        if (pendingUiLayout) TryBuildClientLayout();
    }

    private void TryBuildClientLayout()
    {
        if (PlayerUIManager.Instance == null)
        {
            if (Time.unscaledTime > pendingUiDeadline) pendingUiLayout = false;
            return;
        }

        PlayerUIManager.Instance.BuildLayout(new List<ulong>(pendingUiOrder));
        pendingUiLayout = false;
        Debug.Log("[TurnManager] 客户端 UI 布局刷新成功。");
    }

    // --- 回合流转逻辑 ---
    public void GoToNextTurn()
    {
        if (!IsServer || playerOrder.Count == 0) return;

        int currentIndex = playerOrder.IndexOf(CurrentActivePlayerId.Value);
        int nextIndex = (currentIndex + 1) % playerOrder.Count;

        // 终局判定：回到一号位且是最后一轮
        if (nextIndex == 0 && IsLastRound.Value)
        {
            CalculateWinnerAndEndGame();
            return;
        }

        CurrentActivePlayerId.Value = playerOrder[nextIndex];
    }

    private void CalculateWinnerAndEndGame()
    {
        ulong winnerId = ulong.MaxValue;
        int highestScore = -1;
        int lowestCardCount = int.MaxValue;

        foreach (ulong clientId in playerOrder)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                Player p = client.PlayerObject.GetComponent<Player>();
                if (p != null)
                {
                    int pScore = p.Score.Value;
                    var d = p.Discounts.Value;
                    int pCardCount = d.White + d.Blue + d.Green + d.Red + d.Black;

                    if (pScore > highestScore || (pScore == highestScore && pCardCount < lowestCardCount))
                    {
                        highestScore = pScore;
                        lowestCardCount = pCardCount;
                        winnerId = clientId;
                    }
                }
            }
        }

        NotifyGameEndClientRpc(winnerId, highestScore);
    }

    [ClientRpc]
    private void NotifyGameEndClientRpc(ulong winnerId, int winningScore)
    {
        GameEvents.OnGameEnded?.Invoke(winnerId, winningScore);
        Debug.Log($"[Game Over] 赢家是玩家: {winnerId}");
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}