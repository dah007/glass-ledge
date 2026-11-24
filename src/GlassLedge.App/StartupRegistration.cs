using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using GlassLedge.Core.Services;
using Microsoft.Win32;

namespace GlassLedge.App;

/// <summary>Reads and writes the sign-in startup entry (HKCU Run). Store (MSIX) installs can't use it.</summary>
internal static class StartupRegistration
{
    /// <summary>
    /// True when running from an MSIX package. Registry writes there are redirected to a private copy that
    /// Windows never reads at sign-in, so the Store build is switched on and off in Settings > Apps > Startup.
    /// </summary>
    internal static bool IsPackaged
    {
        get
        {
            var length = 0;
            // 15700 = APPMODEL_ERROR_NO_PACKAGE: a normal, unpackaged process.
            return GetCurrentPackageFullName(ref length, null) != 15700;
        }
    }

    internal static bool IsEnabled()
    {
        if (Environment.ProcessPath is not { } exe)
        {
            return false;
        }

        using var key = Registry.CurrentUser.OpenSubKey(StartupEntry.RunKeyPath);
        return StartupEntry.IsEnabled(key?.GetValue(StartupEntry.ValueName), exe);
    }

    /// <summary>Returns false when Windows refused the change.</summary>
    internal static bool TrySet(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(StartupEntry.RunKeyPath);
            if (enabled)
            {
                if (Environment.ProcessPath is not { } exe)
                {
                    return false;
                }

                key.SetValue(StartupEntry.ValueName, StartupEntry.Command(exe), RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(StartupEntry.ValueName, throwOnMissingValue: false);
            }

            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            return false;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, char[]? packageFullName);
}
