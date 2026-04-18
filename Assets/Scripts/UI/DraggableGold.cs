using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableGold : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    // 永远记住银行里的老家，防止反复拖拽导致老家坐标错乱
    private Vector3 bankHomePosition;
    private Transform bankHomeParent;
    private Transform dragCanvas;

    [Header("吸附与飞回速度")]
    public float snapSpeed = 15f;

    [Header("UI 联动")]
    public Button confirmReserveButton; // 去 Inspector 里把确认预约按钮拖进来！

    // 状态机：当前意图预约的卡牌 ID
    private int pendingCardId = -1;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        dragCanvas = GameObject.Find("Canvas").transform;

        // 游戏启动时，把当前位置焊死为“老家”
        bankHomePosition = rectTransform.position;
        bankHomeParent = transform.parent;

        if (confirmReserveButton != null) confirmReserveButton.interactable = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // --- 0. 硬核拦截：不是你的回合，手给你打断 ---
        if (TurnManager.Instance != null && Unity.Netcode.NetworkManager.Singleton != null)
        {
            if (TurnManager.Instance.CurrentActivePlayerId.Value != Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                GameEvents.OnShowWarningMsg?.Invoke("还没轮到你，别乱摸银行的黄金！");
                eventData.pointerDrag = null; // 物理阻止拖拽
                return;
            }
        }

        // --- 1. 硬核拦截：满 3 张直接打断施法 ---
        Player p = GetLocalPlayer();
        if (p != null)
        {
            var r = p.Reserved.Value;
            int count = (r.Slot1 != -1 ? 1 : 0) + (r.Slot2 != -1 ? 1 : 0) + (r.Slot3 != -1 ? 1 : 0);
            if (count >= 3)
            {
                GameEvents.OnShowWarningMsg?.Invoke("预约位已满3张，无法继续摸黄金！");
                eventData.pointerDrag = null;
                return;
            }
        }

        // --- 2. 冲突覆盖：只要摸了黄金，掀翻拿币的购物车 ---
        if (BankUI.Instance != null && BankUI.Instance.HasPendingDraft())
        {
            BankUI.Instance.ClearSelection();
        }

        // --- 3. 正常拖拽逻辑 ---
        pendingCardId = -1;
        if (confirmReserveButton != null) confirmReserveButton.interactable = false;

        transform.SetParent(dragCanvas);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 如果松手时 pendingCardId 依然是 -1，说明丢到了空地上
        if (pendingCardId == -1)
        {
            StartCoroutine(FlyBackToBank());
        }
    }

    // 接收 CardDropZone 传来的目标坐标和 ID
    public void OnSuccessfulDrop(Transform targetCard, int cardId)
    {
        pendingCardId = cardId;

        // 意图锁定，点亮确认按钮！
        if (confirmReserveButton != null) confirmReserveButton.interactable = true;

        Vector3 targetPos = targetCard.position; // 值传递防爆栈
        StartCoroutine(SnapToCardAndWait(targetPos));
    }

    // ==========================================
    // 留给 UI 按钮调用的公开接口
    // ==========================================

    /// <summary>
    /// 绑在“确认预约”按钮的 OnClick 上
    /// </summary>
    public void ConfirmReservation()
    {
        if (pendingCardId == -1) return;

        Debug.Log($"[DraggableGold] 确认发射！请求预约卡牌 ID: {pendingCardId}");

        // 正式发射全局事件
        GameEvents.OnReserveCardReq?.Invoke(pendingCardId);

        // 卸磨杀驴，清理状态并飞回
        pendingCardId = -1;
        if (confirmReserveButton != null) confirmReserveButton.interactable = false;
        StartCoroutine(FlyBackToBank());
    }

    /// <summary>
    /// 绑在“取消”按钮的 OnClick 上 (可选)
    /// </summary>
    public void CancelReservation()
    {
        pendingCardId = -1;
        if (confirmReserveButton != null) confirmReserveButton.interactable = false;
        StartCoroutine(FlyBackToBank());
    }

    // --- 动画协程 ---

    private IEnumerator FlyBackToBank()
    {
        canvasGroup.blocksRaycasts = false;
        while (Vector3.Distance(rectTransform.position, bankHomePosition) > 1f)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, bankHomePosition, Time.deltaTime * snapSpeed);
            yield return null;
        }
        ResetToBank();
    }

    private IEnumerator SnapToCardAndWait(Vector3 targetPos)
    {
        canvasGroup.blocksRaycasts = false;
        while (Vector3.Distance(rectTransform.position, targetPos) > 1f)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, targetPos, Time.deltaTime * (snapSpeed * 1.5f));
            yield return null;
        }

        // 【关键】：吸附完成后，恢复射线阻挡！
        // 这样玩家如果反悔了，可以直接把卡面上的黄金重新拖走
        canvasGroup.blocksRaycasts = true;
    }

    private void ResetToBank()
    {
        transform.SetParent(bankHomeParent);
        rectTransform.position = bankHomePosition;
        canvasGroup.blocksRaycasts = true;
    }
    // 在 DraggableGold 类里随便找个地方加上这个获取指针的方法
    private Player GetLocalPlayer()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient)
        {
            var localObj = Unity.Netcode.NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (localObj != null) return localObj.GetComponent<Player>();
        }
        return null;
    }
}