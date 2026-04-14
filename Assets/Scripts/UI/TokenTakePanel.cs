using UnityEngine;

public class TokenTakePanel : MonoBehaviour
{
    // 统一入口：UI只发请求，不做本地扣增。
    // 参数顺序固定为: 白,蓝,绿,红,黑
    public void RequestTakeTokens(int white, int blue, int green, int red, int black)
    {
        GameEvents.OnTakeTokensReq?.Invoke(new int[] { white, blue, green, red, black });
    }

    // 下面这些方法可直接绑定到Button，给你快速联调。
    public void TakeThreeDifferent_WBG()
    {
        RequestTakeTokens(1, 1, 1, 0, 0);
    }

    public void TakeThreeDifferent_WRB()
    {
        RequestTakeTokens(1, 0, 1, 1, 0);
    }

    public void TakeTwoWhite()
    {
        RequestTakeTokens(2, 0, 0, 0, 0);
    }

    public void TakeTwoBlue()
    {
        RequestTakeTokens(0, 2, 0, 0, 0);
    }

    public void TakeTwoGreen()
    {
        RequestTakeTokens(0, 0, 2, 0, 0);
    }

    public void TakeTwoRed()
    {
        RequestTakeTokens(0, 0, 0, 2, 0);
    }

    public void TakeTwoBlack()
    {
        RequestTakeTokens(0, 0, 0, 0, 2);
    }
}
