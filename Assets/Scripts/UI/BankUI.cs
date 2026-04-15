using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void Start()
    {
        // 初始化刷新暂存区及按钮默认状态
        ClearSelection();
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

            for (int i = 0; i < remainingTokens.Length && i < bankTokenTexts.Length; i++)
            {
                if (bankTokenTexts[i] != null)
                {
                    bankTokenTexts[i].text = remainingTokens[i].ToString();
                }
            }
        }

        if (bankGoldText != null)
        {
            bankGoldText.text = remainingGold.ToString();
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
        UpdateConfirmButtonState();
    }

    /// <summary>
    /// 确认拿取代币（绑定到“确认拿取”按钮）
    /// </summary>
    public void ConfirmTakeTokens()
    {
        // 发送给网络层或逻辑核心进行校验与扣费
        GameEvents.OnTakeTokensReq?.Invoke(selectedTokens);

        // 重置暂存区，避免重复点击
        ClearSelection();
    }

    /// <summary>
    /// 清空选中的代币（绑定到“取消/重选”按钮）
    /// </summary>
    public void ClearSelection()
    {
        selectedTokens = new int[5];
        RefreshSelectedUI();
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

        // 合法拿取整套提取条件：3个颜色各1个(无同色)，或2个同色
        bool isCompleteDraft = (total == 3 && !hasDouble) || (total == 2 && hasDouble);
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
