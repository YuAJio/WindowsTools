using System.Reflection;

namespace Klick;

/// <summary>
/// 从嵌入式资源 png 生成 Icon (⁎⁍̴̛ᴗ⁍̴̛⁎)
/// 完全不依赖外部文件，单文件发布也能用。
/// </summary>
internal static class IconHelper
{
    public static Icon LoadFromResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return SystemIcons.Application;

        using var bmp = new Bitmap(stream);
        return Icon.FromHandle(bmp.GetHicon());
    }
}
