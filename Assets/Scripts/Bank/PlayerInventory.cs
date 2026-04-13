using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    // 玩家手中的实体代币 (全网同步，只有服务器能修改)
    public NetworkVariable<int> MyEmeralds = new NetworkVariable<int>(0);
    public NetworkVariable<int> MySapphires = new NetworkVariable<int>(0);
    public NetworkVariable<int> MyRubies = new NetworkVariable<int>(0);
    public NetworkVariable<int> MyDiamonds = new NetworkVariable<int>(0);
    public NetworkVariable<int> MyOnyx = new NetworkVariable<int>(0);
    public NetworkVariable<int> MyGold = new NetworkVariable<int>(0);

    // 预留：玩家买下的卡牌提供的“永久宝石减免”
    public NetworkVariable<int> BonusEmeralds = new NetworkVariable<int>(0);
    // ... 其他颜色的 Bonus

    // 预留：玩家的总分数
    public NetworkVariable<int> TotalScore = new NetworkVariable<int>(0);

    // 这是一个供服务器调用的本地方法，把银行扣的钱加到玩家身上
    public void AddTokensServerSide(int em, int sa, int ru, int di, int on)
    {
        if (!IsServer) return; // 再次确保只有服务器能执行

        MyEmeralds.Value += em;
        MySapphires.Value += sa;
        MyRubies.Value += ru;
        MyDiamonds.Value += di;
        MyOnyx.Value += on;
    }
}