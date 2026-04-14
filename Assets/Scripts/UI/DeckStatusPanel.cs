using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DeckStatusPanel : MonoBehaviour
{
    [Header("每层牌堆剩余数量")]
    public TextMeshProUGUI tier1RemainText;
    public TextMeshProUGUI tier2RemainText;
    public TextMeshProUGUI tier3RemainText;
    private bool isBound;

    private void OnEnable()
    {
        TryBindEvents();
        Refresh();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBindEvents();
            Refresh();
        }
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void TryBindEvents()
    {
        if (isBound) return;
        if (MarketDeckManager.Instance == null) return;

        MarketDeckManager.Instance.Tier1DeckRemaining.OnValueChanged += OnRemainChanged;
        MarketDeckManager.Instance.Tier2DeckRemaining.OnValueChanged += OnRemainChanged;
        MarketDeckManager.Instance.Tier3DeckRemaining.OnValueChanged += OnRemainChanged;
        isBound = true;
    }

    private void UnbindEvents()
    {
        if (!isBound) return;
        if (MarketDeckManager.Instance == null) return;

        MarketDeckManager.Instance.Tier1DeckRemaining.OnValueChanged -= OnRemainChanged;
        MarketDeckManager.Instance.Tier2DeckRemaining.OnValueChanged -= OnRemainChanged;
        MarketDeckManager.Instance.Tier3DeckRemaining.OnValueChanged -= OnRemainChanged;
        isBound = false;
    }

    private void OnRemainChanged(int _, int __)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (MarketDeckManager.Instance == null) return;

        if (tier1RemainText != null)
        {
            tier1RemainText.text = MarketDeckManager.Instance.Tier1DeckRemaining.Value.ToString();
        }

        if (tier2RemainText != null)
        {
            tier2RemainText.text = MarketDeckManager.Instance.Tier2DeckRemaining.Value.ToString();
        }

        if (tier3RemainText != null)
        {
            tier3RemainText.text = MarketDeckManager.Instance.Tier3DeckRemaining.Value.ToString();
        }
    }
}
