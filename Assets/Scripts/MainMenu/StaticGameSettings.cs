using UnityEngine;

/// <summary>
/// 这是一个纯静态类，不需要挂在任何物体上。
/// 它像一个“共享仓库”，所有场景都能存取里面的数据。
/// </summary>
public static class StaticGameSettings
{
    // 玩家选择的房间人数（默认 2 人）
    public static int DesiredPlayerCount = 2;

    // 玩家输入的服务器 IP（默认本地）
    public static string TargetServerIP = "127.0.0.1";

    // 可以在这里加更多设置，比如玩家昵称
    public static string PlayerName = "User";
}