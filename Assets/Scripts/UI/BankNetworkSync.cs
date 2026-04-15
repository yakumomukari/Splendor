using UnityEngine;

public class BankNetworkSync : MonoBehaviour
{
    [Header("UI")]
    public BankUI bankUI;

    private bool isBound;

    private void Awake()
    {
        if (bankUI == null)
        {
            bankUI = GetComponent<BankUI>();
        }
    }

    private void OnEnable()
    {
        TryBind();
        RefreshNow();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBind();
            RefreshNow();
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryBind()
    {
        if (isBound) return;
        if (bankUI == null) return;
        if (BankManager.Instance == null) return;

        BankManager.Instance.DiamondCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.SapphireCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.EmeraldCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.RubyCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.OnyxCount.OnValueChanged += OnBankChanged;
        BankManager.Instance.GoldCount.OnValueChanged += OnBankChanged;
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound) return;
        if (BankManager.Instance == null) return;

        BankManager.Instance.DiamondCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.SapphireCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.EmeraldCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.RubyCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.OnyxCount.OnValueChanged -= OnBankChanged;
        BankManager.Instance.GoldCount.OnValueChanged -= OnBankChanged;
        isBound = false;
    }

    private void OnBankChanged(int _, int __)
    {
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (bankUI == null) return;
        if (BankManager.Instance == null) return;

        // 顺序统一为: 白,蓝,绿,红,黑
        int[] remaining = new int[5]
        {
            BankManager.Instance.DiamondCount.Value,
            BankManager.Instance.SapphireCount.Value,
            BankManager.Instance.EmeraldCount.Value,
            BankManager.Instance.RubyCount.Value,
            BankManager.Instance.OnyxCount.Value
        };

        bankUI.UpdateBank(remaining, BankManager.Instance.GoldCount.Value);
    }
}
