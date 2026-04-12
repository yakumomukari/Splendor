using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI pointsText;         // 威望分数文本
    public TextMeshProUGUI bonusGemText;       // 奖励宝石类型的表现（实际开发中可能是 Image）
    
    [Header("Cost UI Elements")]
    public TextMeshProUGUI costWhiteText;      // 可以根据实际情况考虑用数组结构
    public TextMeshProUGUI costBlueText;
    public TextMeshProUGUI costGreenText;
    public TextMeshProUGUI costRedText;
    public TextMeshProUGUI costBlackText;

    // 缓存当前卡牌的 ID，用于点击购买时发送事件
    private int currentCardId;

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
            bonusGemText.text = data.bonusGem.ToString();
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
            // 费用为0时隐藏该UI
            uiText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 购买按钮点击事件（绑定到预制体的 Button 的 OnClick 上）
    /// </summary>
    public void OnBuyClick()
    {
        // UI 只负责“发射人的意图”，不用管现在是不是他的回合，也不去扣自己的钱
        // 这一步彻底实现 UI 和 核心网络与逻辑的解耦！
        GameEvents.OnBuyCardReq?.Invoke(currentCardId);
    }
}
