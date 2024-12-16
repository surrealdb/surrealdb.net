#if !DEBUG && !BENCHMARK_MODE
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SurrealDb.Embedded.Internals;

internal static unsafe partial class NativeMethods
{
    // https://docs.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform
    // Library path will search
    // win => __DllName, __DllName.dll
    // linux, osx => __DllName.so, __DllName.dylib

    static NativeMethods()
    {
        NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, DllImportResolver);
    }

    static IntPtr DllImportResolver(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath
    )
    {
        if (libraryName == __DllName)
        {
            if (GetDefaultPlatformLibraryPath(out var path) && File.Exists(path))
            {
                return NativeLibrary.Load(path, assembly, searchPath);
            }

            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        return IntPtr.Zero;
    }

    static bool GetDefaultPlatformLibraryPath(out string path)
    {
        var pathBuilder = new StringBuilder("runtimes/");

        if (PlatformConfiguration.IsWindows)
            pathBuilder.Append("win-");
        else if (PlatformConfiguration.IsMac)
            pathBuilder.Append("osx-");
        else
            pathBuilder.Append("linux-");

        switch (RuntimeInformation.ProcessArchitecture)
        {
            case Architecture.X86:
                pathBuilder.Append("x86");
                break;
            case Architecture.X64:
                pathBuilder.Append("x64");
                break;
            case Architecture.Arm64:
                pathBuilder.Append("arm64");
                break;
            case Architecture.Arm:
                pathBuilder.Append("arm");
                break;
            default:
                path = string.Empty;
                return false;
        }

        pathBuilder.Append("/native/");
        pathBuilder.Append(__DllName);
        pathBuilder.Append(GetLibraryExtension());

        path = Path.Combine(AppContext.BaseDirectory, pathBuilder.ToString());
        return true;
    }

    static string GetLibraryExtension()
    {
        if (PlatformConfiguration.IsWindows)
            return ".dll";
        if (PlatformConfiguration.IsMac)
            return ".dylib";
        return ".so";
    }
}
#endif
