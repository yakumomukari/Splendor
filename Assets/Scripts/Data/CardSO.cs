using UnityEngine;

// 宝石的类型，按《璀璨宝石》的5种原石加上黄金计算
public enum GemType
{
    White, Blue, Green, Red, Black, Gold
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Splendor/Card Data")]
public class CardSO : ScriptableObject
{
    [Header("基本信息")]
    public int id;
    public int level; // 卡牌等级: 1, 2, 3
    
    [Header("收益")]
    public int points; // 提供的威望分数
    public GemType bonusGem; // 提供的永久宝石折扣颜色
    
    [Header("购买花费")]
    public int costWhite;
    public int costBlue;
    public int costGreen;
    public int costRed;
    public int costBlack;
    // 黄金作为万能资源替代，自身不需要作为花费设定
}
