using TMPro;
using UnityEngine;

public class NobleItemUI : MonoBehaviour
{
    [Header("基础信息")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI pointsText;

    [Header("需求文本(白蓝绿红黑)")]
    public TextMeshProUGUI[] requirementTexts = new TextMeshProUGUI[5];

    public void Setup(NobleSO noble)
    {
        if (noble == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (titleText != null) titleText.text = $"领主 #{noble.id}";
        if (pointsText != null) pointsText.text = noble.points.ToString();

        SetRequirement(0, noble.reqWhite);
        SetRequirement(1, noble.reqBlue);
        SetRequirement(2, noble.reqGreen);
        SetRequirement(3, noble.reqRed);
        SetRequirement(4, noble.reqBlack);
    }

    private void SetRequirement(int index, int value)
    {
        if (index < 0 || index >= requirementTexts.Length) return;

        TextMeshProUGUI t = requirementTexts[index];
        if (t == null) return;

        if (value > 0)
        {
            t.gameObject.SetActive(true);
            t.text = value.ToString();
        }
        else
        {
            if (t != null)
            {
                Transform parent = t.transform.parent;
                if (parent != null)
                {
                    parent.gameObject.SetActive(false); // 隐藏上一级父物体
                }
                else
                {
                    t.gameObject.SetActive(false); // 没有父物体时退化为隐藏自己
                }
            }
        }
    }
}
