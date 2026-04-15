using UnityEngine;
using TMPro;

public class DeckUI : MonoBehaviour
{
    [Header("UI 绑定")]
    public GameObject cardVisual;       // 用于显示卡牌背景/背面的节点
    public TextMeshProUGUI countText;   // 用于显示剩余卡牌数量的文本

    /// <summary>
    /// 更新牌堆剩余数量显示
    /// 纯表现层，绝对不包含任何网络逻辑
    /// </summary>
    /// <param name="count">当前牌堆剩余卡牌数</param>
    public void UpdateCount(int count)
    {
        // 1. 更新显示的数字
        if (countText != null)
        {
            countText.text = count.ToString();
        }

        // 2. 视觉暗示：如果数量 <= 0，则隐藏卡牌背面（或将其置灰等）；否则保持显示
        if (cardVisual != null)
        {
            cardVisual.SetActive(count > 0);
        }
    }
}
