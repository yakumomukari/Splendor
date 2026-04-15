using System;

// 璀璨宝石全局事件总线 (纯静态，绝对不要挂在物体上)
// 团队开发铁律：B(UI层) 只准调用 Invoke() 发射事件，A和C(网络与逻辑层) 只准绑定 += 和 -= 监听事件
public static class GameEvents
{
    // ==========================================
    // 核心玩家操作请求 (UI发射 -> 网络层/逻辑层接收)
    // ==========================================

    // 请求拿取宝石代币
    // 参数说明：传入一个长度为 5 (或6, 包含黄金) 的 int 数组，代表各颜色要拿的数量
    // 例如：拿红、绿、蓝各1个，UI端传 [1, 1, 1, 0, 0, 0]
    public static Action<int[]> OnTakeTokensReq;

    // 请求购买卡牌
    // 参数说明：传入目标卡牌的全局唯一 ID (cardId)
    public static Action<int> OnBuyCardReq;

    // 请求预约卡牌 (明面上的卡或牌堆盲抽)
    // 参数说明：传入卡牌 ID，如果是牌堆盲抽，约定传 -1 等特殊值
    public static Action<int> OnReserveCardReq;

    // 请求归还代币 (当玩家手头代币超过10个时触发的强制操作，阻断逻辑用)
    // 参数说明：传入准备归还的代币数组
    public static Action<int[]> OnReturnTokensReq;


    // 触发条件：服务器检测到玩家同时满足多名贵族，发消息让 UI 弹窗
    // 参数：传入玩家选中的贵族 ID
    public static Action<int> OnChooseNobleReq;
    // 玩家在房间内点击“准备”或房主点击“开始游戏”
    public static Action OnPlayerReadyReq;

    // ==========================================
    // 瞬时反馈 (逻辑层发射 -> UI接收)
    // ==========================================

    // 注意：玩家分数、资产的刷新不需要写在这，直接用 NGO 的 NetworkVariable.OnValueChanged
    // 这里只管瞬时的无状态事件，比如弹出警告
    public static Action<string> OnShowWarningMsg; // 例如："这不是你的回合" 或 "代币不足"

    // ==========================================
    // 🟢 新增：服务器内部解耦广播 (服务器发送 -> 服务器接收)
    // ==========================================

    // 当银行成功扣款后广播。
    // 参数1：拿钱的玩家 ID (ulong)；参数2：拿取的各颜色数量 (int[])
    public static Action<ulong, int[]> OnServerTokensTaken;

    // 当银行驳回拿钱请求时广播。
    // 参数1：犯规的玩家 ID (ulong)；参数2：警告信息 (string)
    public static Action<ulong, string> OnServerTakeTokensFailed;
    // 广播某张卡被买走了，UI 负责监听并销毁那张卡
    public static Action<int> OnServerCardBought;

    // 服务器发现你代币超载，强制要求你还钱 (逻辑层 -> UI层)
    // 参数：需要还的数量
    public static Action<int> OnClientMustReturnTokens;
    public static Action<ulong, int> OnGameEnded;
}