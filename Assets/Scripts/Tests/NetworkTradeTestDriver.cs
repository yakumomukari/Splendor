using UnityEngine;

public class NetworkTradeTestDriver : MonoBehaviour
{
    [Header("测试开关")]
    public bool enableHotkeys = true;

    private void Update()
    {
        if (!enableHotkeys) return;

        // 合法: 白蓝绿各1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            GameEvents.OnTakeTokensReq?.Invoke(new int[] { 1, 1, 1, 0, 0 });
            Debug.Log("[NetTest] F1: 请求拿币 白蓝绿各1");
        }

        // 合法条件依赖库存: 白2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            GameEvents.OnTakeTokensReq?.Invoke(new int[] { 2, 0, 0, 0, 0 });
            Debug.Log("[NetTest] F2: 请求拿币 白2");
        }

        // 非法: 超过3个
        if (Input.GetKeyDown(KeyCode.F3))
        {
            GameEvents.OnTakeTokensReq?.Invoke(new int[] { 1, 1, 1, 1, 0 });
            Debug.Log("[NetTest] F3: 请求非法拿币 4个");
        }

        // 预约卡: 示例ID 101（需场上存在）
        if (Input.GetKeyDown(KeyCode.F4))
        {
            GameEvents.OnReserveCardReq?.Invoke(101);
            Debug.Log("[NetTest] F4: 请求预约卡 101");
        }

        // 买卡: 示例ID 101（需可买）
        if (Input.GetKeyDown(KeyCode.F5))
        {
            GameEvents.OnBuyCardReq?.Invoke(101);
            Debug.Log("[NetTest] F5: 请求买卡 101");
        }

        // 还币: 白1（用于验证还币RPC链路）
        if (Input.GetKeyDown(KeyCode.F6))
        {
            GameEvents.OnReturnTokensReq?.Invoke(new int[] { 1, 0, 0, 0, 0 });
            Debug.Log("[NetTest] F6: 请求还币 白1");
        }
    }
}
