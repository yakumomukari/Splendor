using UnityEngine;
using TMPro;

public class BankUI : MonoBehaviour
{
    [Header("银行存量 (顺序: 白,蓝,绿,红,黑)")]
    public TextMeshProUGUI[] bankTokenTexts = new TextMeshProUGUI[5];
    public TextMeshProUGUI bankGoldText;

    [Header("玩家暂存区 (顺序: 白,蓝,绿,红,黑)")]
    public TextMeshProUGUI[] selectedTokenTexts = new TextMeshProUGUI[5];
    
    // 玩家当前选中的代币
    private int[] selectedTokens = new int[5];

    private void Start()
    {
        // 初始化刷新暂存区显示
        RefreshSelectedUI();
    }

    /// <summary>
    /// 刷新银行中各代币的剩余数量显示
    /// 供服务器状态同步或本地刷新时调用
    /// </summary>
    public void UpdateBank(int[] remainingTokens, int remainingGold)
    {
        if (remainingTokens != null)
        {
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

        // 【注意】这里只做纯粹的UI暂存点击收集。
        // 关于“不能拿超过3个不同色”、“不能拿超过2个相同色”、“存量不足4个不能拿两颗同色”的严密判断，
        // 应该放到网络端由服务器权威防作弊计算，UI 只负责组织成意图发送。
        selectedTokens[colorIndex]++;
        RefreshSelectedUI();
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
