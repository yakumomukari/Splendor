using UnityEngine;

[CreateAssetMenu(fileName = "NewNoble", menuName = "Splendor/Noble")]
public class NobleData : ScriptableObject
{
    [Header("基本信息")]
    public int nobleID;
    public int prestigePoints = 3; // 贵族固定提供 3 分

    [Header("获取条件 (需要的永久折扣数量)")]
    [Tooltip("数组顺序必须严格对应: 白, 蓝, 绿, 红, 黑")]
    public int[] requirements = new int[5];
}
