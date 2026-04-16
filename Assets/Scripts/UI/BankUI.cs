using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class BankUI : MonoBehaviour
{
    [Header("银行存量 (顺序: 白,蓝,绿,红,黑)")]
    public TextMeshProUGUI[] bankTokenTexts = new TextMeshProUGUI[5];
    public TextMeshProUGUI bankGoldText;

    [Header("玩家暂存区 (顺序: 白,蓝,绿,红,黑)")]
    public TextMeshProUGUI[] selectedTokenTexts = new TextMeshProUGUI[5];
    
    [Header("UI 控件")]
    public Button confirmButton;

    // 玩家当前选中的代币
    private int[] selectedTokens = new int[5];
    // 缓存的银行当前存量，用于合法性校验
    private int[] currentBankTokens = new int[5];
    private bool isBound;
    private bool pendingRequest;

    private void OnEnable()
    {
        // 初始化刷新暂存区及按钮默认状态
        ClearSelection();
        TryBindBankEvents();
        RefreshFromBankManager();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBindBankEvents();
            RefreshFromBankManager();
        }
    }

    private void OnDisable()
    {
        UnbindBankEvents();
    }

    private void TryBindBankEvents()
    {
        if (isBound) return;
        if (BankManager.Instance == null) return;

        BankManager.Instance.DiamondCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.SapphireCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.EmeraldCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.RubyCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.OnyxCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.GoldCount.OnValueChanged += OnBankChanged;
        isBound = true;
    }

    private void UnbindBankEvents()
    {
        if (!isBound) return;
        if (BankManager.Instance == null) return;

        BankManager.Instance.DiamondCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.SapphireCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.EmeraldCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.RubyCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.OnyxCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.GoldCount.OnValueChanged -= OnBankChanged;
        isBound = false;
    }

    private void OnBankChanged(int _, int __)
    {
        if (pendingRequest)
        {
            pendingRequest = false;
            ClearSelection();
            return;
        }

        RefreshFromBankManager();
    }

    private void RefreshFromBankManager()
    {
        if (BankManager.Instance == null) return;

        // 顺序统一为: 白,蓝,绿,红,黑
        int[] remaining = new int[5]
        {
            BankManager.Instance.DiamondCount.Value,
            BankManager.Instance.SapphireCount.Value,
            BankManager.Instance.EmeraldCount.Value,
            BankManager.Instance.RubyCount.Value,
            BankManager.Instance.OnyxCount.Value
        };

        UpdateBank(remaining, BankManager.Instance.GoldCount.Value);
    }

    /// <summary>
    /// 刷新银行中各代币的剩余数量显示
    /// 供服务器状态同步或本地刷新时调用
    /// </summary>
    public void UpdateBank(int[] remainingTokens, int remainingGold)
    {
        if (remainingTokens != null)
        {
            remainingTokens.CopyTo(currentBankTokens, 0);
        }

        if (bankGoldText != null)
        {
            bankGoldText.text = remainingGold.ToString();
        }

        // 调用独立的 UI 刷新方法，以此计算“扣除购物车”后的视觉虚数
        RefreshBankUI();
    }

    /// <summary>
    /// 只刷新页面上银行区域各面板的数字（银行真实库存 - 玩家购物车预选数量）
    /// </summary>
    private void RefreshBankUI()
    {
        for (int i = 0; i < currentBankTokens.Length && i < bankTokenTexts.Length; i++)
        {
            if (bankTokenTexts[i] != null)
            {
                // 核心逻辑：显示数量 = 老板柜台里的量 - 已经放在玩家购物车里的量
                int displayCount = currentBankTokens[i] - selectedTokens[i];
                bankTokenTexts[i].text = displayCount.ToString();
            }
        }
    }

    /// <summary>
    /// 当玩家点击某种类型代币时调用 (绑定到 UI 按钮，传入 0~4)
    /// </summary>
    public void SelectToken(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex >= 5) return;

        // 以前纯发意图的做法现在升级加入了【客户端预判】，过滤非法拼装提高体验
        selectedTokens[colorIndex]++;
        
        if (!GameRules.IsValidTokenDraft(selectedTokens, currentBankTokens))
        {
            // 校验不通过：这颗拿得不合法，把它退回去
            selectedTokens[colorIndex]--;
            return;
        }

        RefreshSelectedUI();
        RefreshBankUI(); // 【新增】点这颗宝石时，除了购物车增加，银行的字也要实时变少
        UpdateConfirmButtonState();
    }

    /// <summary>
    /// 确认拿取代币（绑定到“确认拿取”按钮）
    /// </summary>
    public void ConfirmTakeTokens()
    {
        int total = 0;
        for (int i = 0; i < selectedTokens.Length; i++) total += selectedTokens[i];
        if (total <= 0)
        {
            Debug.LogWarning("[BankUI] 你还没选择任何代币。");
            return;
        }

        if (TurnManager.Instance != null && NetworkManager.Singleton != null)
        {
            ulong activeId = TurnManager.Instance.CurrentActivePlayerId.Value;
            ulong localId = NetworkManager.Singleton.LocalClientId;
            if (activeId != localId)
            {
                Debug.LogWarning($"[BankUI] 不是你的回合。当前回合玩家={activeId}，你是={localId}");
                return;
            }
        }

        Debug.Log($"[BankUI] ConfirmTakeTokens: W={selectedTokens[0]} B={selectedTokens[1]} G={selectedTokens[2]} R={selectedTokens[3]} K={selectedTokens[4]}");

        bool sent = false;

        // 先走事件链(若本地Player已订阅)
        if (GameEvents.OnTakeTokensReq != null)
        {
            GameEvents.OnTakeTokensReq.Invoke(new int[]
            {
                selectedTokens[0],
                selectedTokens[1],
                selectedTokens[2],
                selectedTokens[3],
                selectedTokens[4]
            });
            sent = true;
        }

        // 兜底：事件链未就绪时，直接向 BankManager 发 RPC
        if (!sent && BankManager.Instance != null)
        {
            BankManager.Instance.RequestTakeTokensServerRpc(
                selectedTokens[2], // green -> em
                selectedTokens[1], // blue  -> sa
                selectedTokens[3], // red   -> ru
                selectedTokens[0], // white -> di
                selectedTokens[4]  // black -> on
            );
            sent = true;
            Debug.Log("[BankUI] 事件链未订阅，已直接走 BankManager RPC 兜底。");
        }

        if (!sent)
        {
            Debug.LogWarning("[BankUI] 拿币请求未发送：未找到可用事件链或 BankManager。");
            return;
        }

        // 不立即清空，等待服务器库存同步后再清空，避免“点完秒回默认”的错觉。
        pendingRequest = true;
    }

    /// <summary>
    /// 清空选中的代币（绑定到“取消/重选”按钮）
    /// </summary>
    public void ClearSelection()
    {
        selectedTokens = new int[5];
        RefreshSelectedUI();
        RefreshBankUI(); // 【新增】取消时/清空时，把之前扣掉的虚拟数字加回面板上
        UpdateConfirmButtonState();
    }

    /// <summary>
    /// 根据当前预选篮子，更新确认按钮的状态
    /// </summary>
    private void UpdateConfirmButtonState()
    {
        if (confirmButton == null) return;

        int total = 0;
        bool hasDouble = false;
        foreach (int count in selectedTokens)
        {
            total += count;
            if (count == 2) hasDouble = true;
        }

        // 基础合法提取条件：3个颜色各1个(无同色)，或2个同色
        bool isCompleteDraft = (total == 3 && !hasDouble) || (total == 2 && hasDouble);

        // 特殊规则修正：如果玩家拿异色的数量不足3个，但银行里确实已经没有别的颜色可以给他拿了，这也是合法的！
        if (!isCompleteDraft && !hasDouble && total > 0 && total < 3)
        {
            int otherAvailableColors = 0;
            for (int i = 0; i < 5; i++)
            {
                // 如果银行某款颜色还有货，且玩家的暂存区里还没拿它
                if (currentBankTokens[i] > 0 && selectedTokens[i] == 0)
                {
                    otherAvailableColors++;
                }
            }

            // 如果其他所有可选颜色都被拿光了，即玩家已经算是“尽力拿满能拿的所有异色”了，允许确认提交。
            if (otherAvailableColors == 0)
            {
                isCompleteDraft = true;
            }
        }

        confirmButton.interactable = isCompleteDraft;
    }

    /// <summary>
    /// 刷新暂存区的 UI 显示
    /// </summary>
    private void RefreshSelectedUI()
    {
        for (int i = 0; i < selectedTokens.Length && i < selectedTokenTexts.Length; i++)
        {
            if (selectedTokenTexts[i] != null)
            {
                selectedTokenTexts[i].text = selectedTokens[i].ToString();
                // 如果数量为 0，你也可以考虑将其设为隐藏以保持界面整洁
            }
        }
    }
}
