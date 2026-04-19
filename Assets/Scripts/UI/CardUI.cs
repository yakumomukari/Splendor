using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI pointsText;         // 威望分数文本
    public TextMeshProUGUI bonusGemText;       // 奖励宝石类型的表现（实际开发中可能是 Image）
    public GameObject lockOverlay;             // 半透明遮罩层
    public Image GemBg;
    [Header("Cost UI Elements")]
    public TextMeshProUGUI costWhiteText;      // 可以根据实际情况考虑用数组结构
    public TextMeshProUGUI costBlueText;
    public TextMeshProUGUI costGreenText;
    public TextMeshProUGUI costRedText;
    public TextMeshProUGUI costBlackText;

    // 缓存当前卡牌的 ID，用于点击购买时发送事件
    private int currentCardId;

    // 在 CardUI.cs 里补上这个
    public int GetCardId() => currentCardId;

    /// <summary>
    /// 根据传入的 CardSO 数据刷新外观
    /// </summary>
    public void Setup(CardSO data)
    {
        if (data == null) return;

        currentCardId = data.id;

        // 1. 设置分数 (如果为 0 可以隐藏，这里简单转为字符串)
        if (pointsText != null)
        {
            pointsText.text = data.points > 0 ? data.points.ToString() : "";
        }

        // 2. 设置宝石奖励 (如果是图片可相应替换 Sprite)
        if (bonusGemText != null)
        {
            if (GemBg != null)
            {
                TMPColorTool.SetImgColor(GemBg, data.bonusGem switch
                {
                    GemType.White => "#FFFFFF",
                    GemType.Blue => "#146FB4",
                    GemType.Green => "#24980C",
                    GemType.Red => "#DC0000",
                    GemType.Black => "#282828",
                    _ => "#FFFFFF"
                });
            }
            bonusGemText.text = data.bonusGem.ToString();
            if (data.bonusGem.ToString() == "White")
            {
                TMPColorTool.SetTxtColor(bonusGemText, "#824016");
            }
        }

        // 3. 设置花费 (为0的花费通常在UI上会隐藏起来)
        UpdateCostUI(costWhiteText, data.costWhite);
        UpdateCostUI(costBlueText, data.costBlue);
        UpdateCostUI(costGreenText, data.costGreen);
        UpdateCostUI(costRedText, data.costRed);
        UpdateCostUI(costBlackText, data.costBlack);
    }

    private void UpdateCostUI(TextMeshProUGUI uiText, int cost)
    {
        if (uiText == null) return;

        if (cost > 0)
        {
            uiText.gameObject.SetActive(true);
            uiText.text = cost.ToString();
        }
        else
        {
            if (uiText != null)
            {
                Transform parent = uiText.transform.parent;
                if (parent != null)
                {
                    parent.gameObject.SetActive(false); // 隐藏上一级父物体
                }
                else
                {
                    uiText.gameObject.SetActive(false); // 没有父物体时退化为隐藏自己
                }
            }
        }
    }

    /// <summary>
    /// 更新交互与遮罩状态
    /// </summary>
    public void SetState(bool canAfford)
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = canAfford;
        }

        if (lockOverlay != null)
        {
            lockOverlay.SetActive(!canAfford);
        }
    }

    /// <summary>
    /// 购买按钮点击事件（绑定到预制体的 Button 的 OnClick 上）
    /// </summary>
    public void OnBuyClicked()
    {
        // 冲突覆盖：点击买卡时，如果购物车里有半成品的代币，直接掀翻
        if (BankUI.Instance != null && BankUI.Instance.HasPendingDraft())
        {
            BankUI.Instance.ClearSelection();
            Debug.Log("[CardUI] 玩家点击购买卡牌，已自动清空代币暂存区。");
        }

        // 发射你的买卡事件
        GameEvents.OnBuyCardReq?.Invoke(currentCardId);
    }
}
