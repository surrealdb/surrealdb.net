using System.Runtime.InteropServices;

namespace SurrealDb.Embedded.Internals;

// 💡 Detect platform, inspired by SkiaSharp implementation
// https://github.com/mono/SkiaSharp/blob/main/binding/Binding.Shared/PlatformConfiguration.cs

internal static class PlatformConfiguration
{
    private const string LibCLibrary = "libc";

    private static bool IsGlibcImplementation()
    {
        try
        {
            gnu_get_libc_version();
            return true;
        }
        catch (TypeLoadException)
        {
            return false;
        }
    }

    [DllImport(LibCLibrary, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr gnu_get_libc_version();

    private static readonly Lazy<bool> _isGlibcLazy = new(IsGlibcImplementation);

    public static bool IsUnix => IsMac || IsLinux;

    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsMac => OperatingSystem.IsMacOS();
    public static bool IsLinux => OperatingSystem.IsLinux();

    public static bool IsArm =>
        RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64;

    public static bool Is64Bit => IntPtr.Size == 8;

    private static string? linuxFlavor;
    public static string? LinuxFlavor
    {
        get
        {
            if (!IsLinux)
                return null;

            if (!string.IsNullOrEmpty(linuxFlavor))
                return linuxFlavor;

            // we only check for musl/glibc right now
            if (!IsGlibc)
                return "musl";

            return null;
        }
        set => linuxFlavor = value;
    }

    public static bool IsGlibc => IsLinux && _isGlibcLazy.Value;
}
