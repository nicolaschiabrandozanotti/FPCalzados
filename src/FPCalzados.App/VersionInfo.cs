using System.Reflection;

namespace FPCalzados.App;

internal static class VersionInfo
{
    public static string ProductName => "FPCalzados";

    public static string Version
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                return informational.Split('+')[0];
            }

            return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        }
    }

    public static string Display => $"{ProductName} v{Version}";
}
