using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(NetworkObject))]
public class NobleManager : NetworkBehaviour
{
    public static NobleManager Instance { get; private set; }

    [Header("开局明置贵族数量")]
    [SerializeField] private int initialFaceUpCount = 3;

    // 删掉 readonly 和 = new ...
    public NetworkList<int> FaceUpNobleIds;

    private readonly Dictionary<int, NobleSO> idToNoble = new Dictionary<int, NobleSO>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 核心补丁：只有预制体真正进入场景时，才分配非托管内存
        FaceUpNobleIds = new NetworkList<int>();

        LoadNobles();
    }

    private void LoadNobles()
    {
#if UNITY_EDITOR
        // 编辑器模式：从 Assets/Settings/Nobles 加载
        string[] guids = AssetDatabase.FindAssets("t:NobleSO", new[] { "Assets/Settings/Nobles" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NobleSO noble = AssetDatabase.LoadAssetAtPath<NobleSO>(path);
            if (noble == null) continue;
            if (idToNoble.ContainsKey(noble.id))
            {
                Debug.LogError($"[Noble] 贵族ID重复: {noble.id}");
                continue;
            }
            idToNoble.Add(noble.id, noble);
        }
        Debug.Log($"[Noble] 编辑器模式加载：共加载 {idToNoble.Count} 个贵族");
#else
        // 运行时模式：从 Resources/Nobles 加载
        NobleSO[] all = Resources.LoadAll<NobleSO>("Nobles");
        foreach (var noble in all)
        {
            if (noble == null) continue;
            if (idToNoble.ContainsKey(noble.id))
            {
                Debug.LogError($"[Noble] 贵族ID重复: {noble.id}");
                continue;
            }
            idToNoble.Add(noble.id, noble);
        }
        Debug.Log($"[Noble] 运行时模式加载：共加载 {idToNoble.Count} 个贵族");
#endif
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Noble] OnNetworkSpawn | IsServer={IsServer} IsClient={IsClient} LoadedNobles={idToNoble.Count}");

        if (!IsServer) return;
        InitializeFaceUpNobles();
    }

    private void InitializeFaceUpNobles()
    {
        if (FaceUpNobleIds.Count > 0) return;

        List<int> ids = new List<int>(idToNoble.Keys);
        ShuffleInPlace(ids);

        int take = Mathf.Min(initialFaceUpCount, ids.Count);
        for (int i = 0; i < take; i++)
        {
            FaceUpNobleIds.Add(ids[i]);
        }

        string nobleIdStr = "";
        for (int i = 0; i < FaceUpNobleIds.Count; i++)
        {
            if (i > 0) nobleIdStr += ", ";
            nobleIdStr += FaceUpNobleIds[i];
        }
        Debug.Log($"[Noble-Server] 明置贵族初始化完成，从 {idToNoble.Count} 个贵族中随机选择 {take} 个，分别是: {nobleIdStr}");
    }

    public void TryGrantNobleToPlayer(Player player)
    {
        if (!IsServer || player == null) return;
        if (FaceUpNobleIds.Count == 0) return;

        int[] discounts = player.Discounts.Value.ToBaseGemArray();

        List<int> claimable = new List<int>();
        for (int i = 0; i < FaceUpNobleIds.Count; i++)
        {
            int nobleId = FaceUpNobleIds[i];
            NobleSO noble = GetNoble(nobleId);
            if (noble == null) continue;

            if (CanClaim(noble, discounts))
            {
                claimable.Add(nobleId);
            }
        }

        if (claimable.Count == 0) return;

        // 暂时自动拿最小ID，后续可接 OnChooseNobleReq 做多贵族弹窗选择。
        claimable.Sort();
        GrantNoble(player, claimable[0]);
    }

    private void GrantNoble(Player player, int nobleId)
    {
        NobleSO noble = GetNoble(nobleId);
        if (noble == null) return;

        bool removed = false;
        for (int i = 0; i < FaceUpNobleIds.Count; i++)
        {
            if (FaceUpNobleIds[i] == nobleId)
            {
                FaceUpNobleIds.RemoveAt(i);
                removed = true;
                break;
            }
        }

        if (!removed) return;

        player.Score.Value += noble.points;
        Debug.Log($"[Noble] 玩家 {player.OwnerClientId} 获得贵族 {nobleId}，+{noble.points} 分。");
    }

    private NobleSO GetNoble(int id)
    {
        idToNoble.TryGetValue(id, out NobleSO noble);
        return noble;
    }

    public NobleSO GetNobleById(int id)
    {
        return GetNoble(id);
    }

    private static bool CanClaim(NobleSO noble, int[] discounts)
    {
        if (discounts == null || discounts.Length < 5) return false;

        return discounts[0] >= noble.reqWhite
            && discounts[1] >= noble.reqBlue
            && discounts[2] >= noble.reqGreen
            && discounts[3] >= noble.reqRed
            && discounts[4] >= noble.reqBlack;
    }

    private static void ShuffleInPlace(List<int> ids)
    {
        for (int i = ids.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = ids[i];
            ids[i] = ids[j];
            ids[j] = t;
        }
    }
    public override void OnDestroy()
    {
        // 游戏结束或物体销毁时，手动把这块非托管内存还给操作系统
        if (FaceUpNobleIds != null)
        {
            FaceUpNobleIds.Dispose();
        }

        base.OnDestroy(); // 千万别忘了基类调用
    }
}
