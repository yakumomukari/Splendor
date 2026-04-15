using UnityEngine;
using Unity.Collections;

public static class LeakDetectionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnableLeakDetection()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
        Debug.Log("[LeakDetection] NativeLeakDetection 已启用 Full StackTrace 模式。");
#endif
    }
}
