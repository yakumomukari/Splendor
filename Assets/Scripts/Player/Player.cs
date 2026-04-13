using Unity.Netcode;
using UnityEngine;

// 1. 结构体升级：加上转数组的方法，迎合 B 的胃口
public struct TokenAssets : INetworkSerializable
{
    // ⚠️ 极其重要：必须严格按照 B 定的顺序：白, 蓝, 绿, 红, 黑, (金)
    // 不然以后买卡绝对扣错钱
    public int White, Blue, Green, Red, Black, Gold;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref White);
        serializer.SerializeValue(ref Blue);
        serializer.SerializeValue(ref Green);
        serializer.SerializeValue(ref Red);
        serializer.SerializeValue(ref Black);
        serializer.SerializeValue(ref Gold);
    }

    // 适配器魔法：给 B 的算法吐出一个长度为 5 的基础宝石数组
    public int[] ToBaseGemArray()
    {
        return new int[] { White, Blue, Green, Red, Black };
    }
}

// 2. 玩家实体升级：补齐折扣字段
public class Player : NetworkBehaviour
{
    public NetworkVariable<int> Score = new NetworkVariable<int>(0);
    
    // 玩家手里的实体代币 (上限10个那个)
    public NetworkVariable<TokenAssets> Tokens = new NetworkVariable<TokenAssets>(
        new TokenAssets(), 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // 新增：玩家买卡积累的永久折扣
    // 为了省事，直接复用 TokenAssets 结构体，它的 Gold 字段永远是 0 就行
    public NetworkVariable<TokenAssets> Discounts = new NetworkVariable<TokenAssets>(
        new TokenAssets(), 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    // ==========================================
    // 给你自己用的高阶判定方法 (在 ServerRpc 里调用)
    // ==========================================
    public bool CanAfford(int[] cardCosts)
    {
        // 直接把结构体转换成 B 需要的数组格式，扔进他写的规则里算
        return GameRules.CanAffordCard(
            Tokens.Value.ToBaseGemArray(),
            Discounts.Value.ToBaseGemArray(),
            Tokens.Value.Gold,
            cardCosts,
            out _
        );
    }
}