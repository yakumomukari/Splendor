using UnityEngine;

[CreateAssetMenu(fileName = "NewNoble", menuName = "Splendor/Noble Data")]
public class NobleSO : ScriptableObject
{
    [Header("基本信息")]
    public int id;
    public int points = 3;

    [Header("拜访要求(永久折扣)")]
    public int reqWhite;
    public int reqBlue;
    public int reqGreen;
    public int reqRed;
    public int reqBlack;
}
