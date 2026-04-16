using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance { get; private set; }

    [Header("UI 预制体 (找 C 组要这三个)")]
    public PlayerPanel localPlayerPanelPrefab;       // 主面板 (横版，尺寸大，图里玩家1)
    public PlayerPanel opponentHorizontalPrefab;     // 对手顶部面板 (横版，尺寸小，图里玩家2/3)
    public PlayerPanel opponentVerticalPrefab;       // 对手侧边面板 (竖版，尺寸小，图里玩家4/5)

    [Header("座位锚点")]
    public Transform anchorBottom;   // 绝对 0 号位: 玩家自己
    public Transform anchorLeft;     // 相对 1 号位 (三人局下家 / 五人局下家)
    public Transform anchorTopLeft;  // 相对 2 号位 (五人局)
    public Transform anchorTop;      // 相对 2 号位 (两人局/四人局对家)
    public Transform anchorTopRight; // 相对 3 号位 (五人局)
    public Transform anchorRight;    // 相对 3/4 号位 (三人/四人/五人局上家)

    private Dictionary<ulong, PlayerPanel> playerPanels = new Dictionary<ulong, PlayerPanel>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void BuildLayout(List<ulong> playerOrder)
    {
        foreach (var panel in playerPanels.Values)
        {
            if (panel != null) Destroy(panel.gameObject);
        }
        playerPanels.Clear();

        int totalPlayers = playerOrder.Count;
        if (totalPlayers < 1 || totalPlayers > 5) return;

        ulong myClientId = NetworkManager.Singleton.LocalClientId;
        int myRealIndex = playerOrder.IndexOf(myClientId);
        if (myRealIndex == -1) myRealIndex = 0;

        for (int i = 0; i < totalPlayers; i++)
        {
            ulong targetId = playerOrder[i];
            int relativeIndex = (i - myRealIndex + totalPlayers) % totalPlayers;

            Transform targetAnchor = GetAnchor(totalPlayers, relativeIndex);

            // 【核心修改】：根据锚点的位置，智能选择横版还是竖版的预制体
            PlayerPanel prefabToUse;
            if (relativeIndex == 0)
            {
                prefabToUse = localPlayerPanelPrefab; // 自己永远用底部的豪华大号横版
            }
            else if (targetAnchor == anchorLeft || targetAnchor == anchorRight)
            {
                prefabToUse = opponentVerticalPrefab; // 左右两侧用【竖版】
            }
            else
            {
                prefabToUse = opponentHorizontalPrefab; // 顶部用【横版】
            }

            PlayerPanel newPanel = Instantiate(prefabToUse, targetAnchor);
            playerPanels.Add(targetId, newPanel);
            Debug.Log($"[UI] 为玩家 {targetId} 生成了面板");
        }

        // ==========================================
        // 【时序补丁】：全员点名，强行接线
        // ==========================================
        // 扫出当前场景里所有已经出生的 Player 实体
        Player[] allPlayers = FindObjectsOfType<Player>();

        foreach (var kvp in playerPanels)
        {
            ulong cid = kvp.Key;
            PlayerPanel panel = kvp.Value;

            foreach (var p in allPlayers)
            {
                // 如果找到了对应 ID 的实体，直接强塞面板
                if (p.OwnerClientId == cid)
                {
                    p.BindUI(panel);
                    break; // 找到了就看下一个面板
                }
            }
        }
    }

    private Transform GetAnchor(int total, int relativeIndex)
    {
        if (relativeIndex == 0) return anchorBottom;

        switch (total)
        {
            case 2: return anchorTop;
            case 3:
                if (relativeIndex == 1) return anchorLeft;
                if (relativeIndex == 2) return anchorRight;
                break;
            case 4:
                if (relativeIndex == 1) return anchorLeft;
                if (relativeIndex == 2) return anchorTop;
                if (relativeIndex == 3) return anchorRight;
                break;
            case 5:
                // 完美契合你的 UI 图：1左，2左上，3右上，4右
                if (relativeIndex == 1) return anchorLeft;
                if (relativeIndex == 2) return anchorTopLeft;
                if (relativeIndex == 3) return anchorTopRight;
                if (relativeIndex == 4) return anchorRight;
                break;
        }
        return anchorBottom;
    }

    public PlayerPanel GetPanelByClientId(ulong clientId)
    {
        playerPanels.TryGetValue(clientId, out PlayerPanel panel);
        return panel;
    }
}