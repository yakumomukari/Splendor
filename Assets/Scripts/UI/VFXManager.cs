using UnityEngine;
using Unity.Netcode;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("特效预制体")]
    public CardFlightVFX dummyCardPrefab; // 把刚才做好的那个预制体拖进来
    public Transform fxCanvas; // 找一个处于最顶层的 Canvas 节点用来放飞行物，防止被遮挡

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayCardFlightFX += HandleCardFlight;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayCardFlightFX -= HandleCardFlight;
    }

    private void HandleCardFlight(int cardId, ulong buyerId, Vector3 startPos)
    {
        // 1. 查字典，这卡是什么颜色的？
        CardSO cardData = GlobalCardDatabase.Instance.GetCard(cardId);
        if (cardData == null) return;

        // 2. 找靶心，买卡的人坐在哪个面板？
        PlayerPanel targetPanel = PlayerUIManager.Instance.GetPanelByClientId(buyerId);
        if (targetPanel == null) return;

        // 3. 拿到具体对应颜色的宝石槽位坐标
        Transform targetSlot = targetPanel.GetDiscountSlotTransform(cardData.bonusGem);

        // 4. 生成替身并开火
        CardFlightVFX dummy = Instantiate(dummyCardPrefab, fxCanvas);

        dummy.PlayFlight(startPos, targetSlot.position, () =>
        {
            // 动画结束时的回调 (可以在这里播放一个“啪”的音效，或者爆个粒子)
            Debug.Log($"[VFX] 卡牌 {cardId} 飞行到位，玩家 {buyerId} 面板即将刷新！");

            // 注意：此时服务器的 RPC 数据大概率已经同步过来了，面板会自动刷新数字
            // 如果你想做那种“飞到了数字才变”的强视觉同步，你需要稍微魔改一下数据绑定的时机，
            // 但按现在这种方式跑，视觉上已经能做到 90 分了。
        });
    }
}