using System.Reflection;

namespace MoodyBlues;

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
