using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TurnStatusPanel : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI currentTurnText;
    public TextMeshProUGUI waitingReturnText;

    private bool isBound;

    private void OnEnable()
    {
        TryBind();
        Refresh();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBind();
            Refresh();
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryBind()
    {
        if (isBound) return;
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.CurrentActivePlayerId.OnValueChanged += OnTurnChanged;
        TurnManager.Instance.IsWaitingForReturn.OnValueChanged += OnWaitingChanged;
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound) return;
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.CurrentActivePlayerId.OnValueChanged -= OnTurnChanged;
        TurnManager.Instance.IsWaitingForReturn.OnValueChanged -= OnWaitingChanged;
        isBound = false;
    }

    private void OnTurnChanged(ulong _, ulong __)
    {
        Refresh();
    }

    private void OnWaitingChanged(bool _, bool __)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (TurnManager.Instance == null)
        {
            if (currentTurnText != null) currentTurnText.text = "Turn: N/A";
            if (waitingReturnText != null) waitingReturnText.text = "Return Lock: N/A";
            return;
        }

        ulong activeId = TurnManager.Instance.CurrentActivePlayerId.Value;
        bool waiting = TurnManager.Instance.IsWaitingForReturn.Value;
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;

        if (currentTurnText != null)
        {
            currentTurnText.text = activeId == localId
                ? $"Turn: You ({activeId})"
                : $"Turn: Player {activeId}";
        }

        if (waitingReturnText != null)
        {
            waitingReturnText.text = waiting ? "Return Lock: ON" : "Return Lock: OFF";
        }
    }
}
