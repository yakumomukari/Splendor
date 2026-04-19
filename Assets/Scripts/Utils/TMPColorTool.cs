using TMPro;
using UnityEngine;
using UnityEngine.UI; // 必须引用这个命名空间才能使用 Image
public static class TMPColorTool
{
    /// <summary>
    /// 使用十六进制字符串设置 TMP 文本颜色，支持 #RRGGBB 和 #RRGGBBAA。
    /// </summary>
    public static bool SetTxtColor(TMP_Text target, string hex, Color? fallbackColor = null)
    {
        if (target == null || string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        if (ColorUtility.TryParseHtmlString(hex, out var parsedColor))
        {
            target.color = parsedColor;
            return true;
        }

        if (fallbackColor.HasValue)
        {
            target.color = fallbackColor.Value;
        }

        return false;
    }
    public static bool SetImgColor(Image target, string hex, Color? fallbackColor = null)
    {
        if (target == null || string.IsNullOrWhiteSpace(hex)) return false;

        if (ColorUtility.TryParseHtmlString(hex, out var parsedColor))
        {
            target.color = parsedColor;
            return true;
        }

        if (fallbackColor.HasValue)
        {
            target.color = fallbackColor.Value;
        }

        return false;
    }
    /// <summary>
    /// 直接使用 RGBA 数值设置 TMP 文本颜色（0-255）。
    /// </summary>
    public static void SetTxtColor(TMP_Text target, byte r, byte g, byte b, byte a = 255)
    {
        if (target == null)
        {
            return;
        }

        target.color = new Color32(r, g, b, a);
    }
}
