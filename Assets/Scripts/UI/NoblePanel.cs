using Unity.Netcode;
using UnityEngine;

public class NoblePanel : MonoBehaviour
{
    [Header("UI")]
    public Transform nobleContainer;
    public NobleItemUI nobleItemPrefab;

    private readonly System.Collections.Generic.List<NobleItemUI> activeItems = new System.Collections.Generic.List<NobleItemUI>();
    private bool isBound;

    private void OnEnable()
    {
        TryBindEvents();
        Rebuild();
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBindEvents();
        }
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void TryBindEvents()
    {
        if (isBound) return;
        if (NobleManager.Instance == null) return;
        NobleManager.Instance.FaceUpNobleIds.OnListChanged += OnNobleListChanged;
        isBound = true;
    }

    private void UnbindEvents()
    {
        if (!isBound) return;
        if (NobleManager.Instance == null) return;
        NobleManager.Instance.FaceUpNobleIds.OnListChanged -= OnNobleListChanged;
        isBound = false;
    }

    private void OnNobleListChanged(NetworkListEvent<int> _)
    {
        Rebuild();
    }

    public void Rebuild()
    {
        Clear();

        if (nobleContainer == null || nobleItemPrefab == null) return;
        if (NobleManager.Instance == null) return;

        var ids = NobleManager.Instance.FaceUpNobleIds;
        for (int i = 0; i < ids.Count; i++)
        {
            NobleSO noble = NobleManager.Instance.GetNobleById(ids[i]);
            if (noble == null) continue;

            NobleItemUI item = Instantiate(nobleItemPrefab, nobleContainer);
            item.Setup(noble);
            activeItems.Add(item);
        }
    }

    private void Clear()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] != null)
            {
                Destroy(activeItems[i].gameObject);
            }
        }
        activeItems.Clear();
    }
}
