using UnityEngine;
using TMPro;

public class PlayerPanel : MonoBehaviour
{
    [Header("代币数量 (顺序: 白,蓝,绿,红,黑)")]
    public TextMeshProUGUI[] tokenTexts = new TextMeshProUGUI[5];

    [Header("永久折扣 (顺序: 白,蓝,绿,红,黑)")]
    public TextMeshProUGUI[] discountTexts = new TextMeshProUGUI[5];

    [Header("特殊资源与积分")]
    public TextMeshProUGUI goldText;    // 黄金数量
    public TextMeshProUGUI scoreText;   // 总威望分数

    private void Awake()
    {
        // 自动查找：如果编辑器没有绑定，就从子物体自动查找
        TryAutoFindTexts();
    }

    private void TryAutoFindTexts()
    {
        // 查找 tokenTexts 和 discountTexts
        if (tokenTexts == null || tokenTexts.Length != 5 || System.Array.Exists(tokenTexts, t => t == null))
        {
            TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>();
            if (allTexts.Length >= 10)
            {
                tokenTexts = new TextMeshProUGUI[5];
                discountTexts = new TextMeshProUGUI[5];
                // 假设前 5 个是 tokenTexts，后 5 个是 discountTexts
                for (int i = 0; i < 5; i++)
                {
                    tokenTexts[i] = allTexts[i];
                    discountTexts[i] = allTexts[i + 5];
                }
                if (allTexts.Length >= 11) goldText = allTexts[10];
                if (allTexts.Length >= 12) scoreText = allTexts[11];
                Debug.Log("[PlayerPanel] 自动查找 UI 文本完成");
            }
        }
    }

    /// <summary>
    /// 纯表现层：更新玩家面板的所有 UI 数据
    /// 绝对不包含任何 Network 或逻辑运算
    /// </summary>
    /// <param name="tokens">玩家拥有的 5 种宝石数量</param>
    /// <param name="discounts">玩家拥有的 5 种永久折扣数量</param>
    /// <param name="gold">玩家拥有的黄金代币数量</param>
    /// <param name="score">玩家当前的总分数</param>
    public void UpdatePlayerUI(int[] tokens, int[] discounts, int gold, int score)
    {
        // 1. 更新 5 种基础代币 UI
        if (tokens != null)
        {
            for (int i = 0; i < tokens.Length && i < tokenTexts.Length; i++)
            {
                if (tokenTexts[i] != null)
                {
                    tokenTexts[i].text = tokens[i].ToString();
                }
            }
        }

        // 2. 更新 5 种永久折扣 UI
        if (discounts != null)
        {
            for (int i = 0; i < discounts.Length && i < discountTexts.Length; i++)
            {
                if (discountTexts[i] != null)
                {
                    discountTexts[i].text = discounts[i].ToString();
                }
            }
        }

        // 3. 更新黄金数量 UI
        if (goldText != null)
        {
            goldText.text = gold.ToString();
        }

        // 4. 更新总积分 UI
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }
}
