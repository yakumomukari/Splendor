using UnityEngine;
using System.Collections;
using System;

// 挂在用于飞行的临时卡牌预制体上
public class CardFlightVFX : MonoBehaviour
{
    [Header("飞行配置")]
    public float flightDuration = 0.4f; // 飞行动画时间
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 缓动曲线
    
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 启动飞行协程
    /// </summary>
    /// <param name="startPos">世界坐标起点</param>
    /// <param name="endPos">世界坐标终点</param>
    /// <param name="onComplete">飞完后的回调函数</param>
    public void PlayFlight(Vector3 startPos, Vector3 endPos, Action onComplete)
    {
        transform.position = startPos;
        StartCoroutine(FlightRoutine(startPos, endPos, onComplete));
    }

    private IEnumerator FlightRoutine(Vector3 start, Vector3 end, Action onComplete)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * 0.3f; // 飞进卡槽时缩小一点更自然

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;
            float curveT = speedCurve.Evaluate(t);

            transform.position = Vector3.Lerp(start, end, curveT);
            transform.localScale = Vector3.Lerp(startScale, endScale, curveT);

            yield return null;
        }

        // 飞完后触发回调并销毁自己
        onComplete?.Invoke();
        Destroy(gameObject);
    }
}