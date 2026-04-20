using Unity.Netcode;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    void Start()
    {
        // 如果是在编辑器里点 Play，我们什么都不自动做，全手动点
        if (Application.isEditor) return;

        // 【服务器专属】只有 Dedicated Server 包才会自动执行
        #if UNITY_SERVER
            Debug.Log("--- Linux 专用服务器启动：自动开启 StartServer ---");
            NetworkManager.Singleton.StartServer();
        #endif

        // 【玩家专属】普通 Player 包（.exe）不会自动开启任何东西
        // 逻辑交给主菜单去触发
    }
}