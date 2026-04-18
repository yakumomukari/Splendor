using UnityEngine;
using UnityEngine.EventSystems;

public class CardDropZone : MonoBehaviour, IDropHandler
{
    private CardUI myCard;

    private void Awake()
    {
        myCard = GetComponent<CardUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableGold gold = eventData.pointerDrag.GetComponent<DraggableGold>();
            if (gold != null)
            {
                int targetCardId = myCard.GetCardId();
                Debug.Log($"[DropZone] 黄金已就位，等待玩家点击确认预约卡牌: {targetCardId}");

                // 告诉黄金吸附并记录 ID，但不发网络请求！
                gold.OnSuccessfulDrop(this.transform, targetCardId);
            }
        }
    }
}