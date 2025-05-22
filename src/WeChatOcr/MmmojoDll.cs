using System.Runtime.InteropServices;

namespace WeChatOcr;

public enum MMMojoInfoMethod
{
    kMMNone = 0,
    kMMPush,
    kMMPullReq,
    kMMPullResp,
    kMMShared
}

public enum MMMojoCallbackType
{
    kMMUserData = 0,
    kMMReadPush,
    kMMReadPull,
    kMMReadShared,
    kMMRemoteConnect,
    kMMRemoteDisconnect,
    kMMRemoteProcessLaunched,
    kMMRemoteProcessLaunchFailed,
    kMMRemoteMojoError
}

public enum MMMojoEnvironmentInitParamType
{
    kMMHostProcess = 0,
    kMMLoopStartThread,
    kMMExePath,
    kMMLogPath,
    kMMLogToStderr,
    kMMAddNumMessagepipe,
    kMMSetDisconnectHandlers,
    kMMDisableDefaultPolicy = 1000,
    kMMElevated,
    kMMCompatible
}

public static class MmmojoDll
{
    private static IntPtr _dllHandle = IntPtr.Zero;
    private static readonly Dictionary<string, Delegate> _delegates = new();

    // Windows API
    [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    // 委托声明
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void InitializeMMMojoDelegate(int argc, IntPtr argv);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ShutdownMMMojoDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateMMMojoEnvironmentDelegate();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetMMMojoEnvironmentCallbacksDelegate(IntPtr mmmojo_env, int type, IntPtr callback);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void SetMMMojoEnvironmentInitParamsDelegate(IntPtr mmmojo_env, int type, IntPtr param);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public delegate void AppendMMSubProcessSwitchNativeDelegate(IntPtr mmmojo_env, IntPtr switchStringPtr, IntPtr valuePtr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void StartMMMojoEnvironmentDelegate(IntPtr mmmojo_env);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void StopMMMojoEnvironmentDelegate(IntPtr mmmojo_env);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RemoveMMMojoEnvironmentDelegate(IntPtr mmmojo_env);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetMMMojoReadInfoRequestDelegate(IntPtr mmmojo_readinfo, ref uint requestDataSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetMMMojoReadInfoAttachDelegate(IntPtr mmmojo_readinfo, ref uint attachDataSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RemoveMMMojoReadInfoDelegate(IntPtr mmmojo_readinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int GetMMMojoReadInfoMethodDelegate(IntPtr mmmojo_readinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool GetMMMojoReadInfoSyncDelegate(IntPtr mmmojo_readinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr CreateMMMojoWriteInfoDelegate(int method, int sync, uint requestId);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetMMMojoWriteInfoRequestDelegate(IntPtr mmmojo_writeinfo, uint requestDataSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RemoveMMMojoWriteInfoDelegate(IntPtr mmmojo_writeinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetMMMojoWriteInfoAttachDelegate(IntPtr mmmojo_writeinfo, uint attachDataSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetMMMojoWriteInfoMessagePipeDelegate(IntPtr mmmojo_writeinfo, int numOfMessagePipe);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SetMMMojoWriteInfoResponseSyncDelegate(IntPtr mmmojo_writeinfo, ref IntPtr mmmojo_readinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool SendMMMojoWriteInfoDelegate(IntPtr mmmojo_env, IntPtr mmmojo_writeinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool SwapMMMojoWriteInfoCallbackDelegate(IntPtr mmmojo_writeinfo, IntPtr mmmojo_readinfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public delegate bool SwapMMMojoWriteInfoMessageDelegate(IntPtr mmmojo_writeinfo, IntPtr mmmojo_readinfo);

    // 加载DLL
    public static void Load(string dllPath)
    {
        if (_dllHandle != IntPtr.Zero)
            return;

        _dllHandle = LoadLibrary(dllPath);
        if (_dllHandle == IntPtr.Zero)
            throw new Exception($"无法加载DLL: {dllPath}");
    }

    // 释放DLL
    public static void Unload()
    {
        if (_dllHandle != IntPtr.Zero)
        {
            FreeLibrary(_dllHandle);
            _dllHandle = IntPtr.Zero;
            _delegates.Clear();
        }
    }

    // 获取委托
    private static T GetFunction<T>(string name) where T : Delegate
    {
        if (_dllHandle == IntPtr.Zero)
            throw new Exception("DLL未加载");

        if (_delegates.TryGetValue(name, out var del))
            return (T)del;

        var proc = GetProcAddress(_dllHandle, name);
        if (proc == IntPtr.Zero)
            throw new Exception($"找不到函数: {name}");

        var func = Marshal.GetDelegateForFunctionPointer<T>(proc);
        _delegates[name] = func;
        return func;
    }

    // 对外静态方法
    public static void InitializeMMMojo(int argc, IntPtr argv)
        => GetFunction<InitializeMMMojoDelegate>("InitializeMMMojo")(argc, argv);

    public static void ShutdownMMMojo()
        => GetFunction<ShutdownMMMojoDelegate>("ShutdownMMMojo")();

    public static IntPtr CreateMMMojoEnvironment()
        => GetFunction<CreateMMMojoEnvironmentDelegate>("CreateMMMojoEnvironment")();

    public static void SetMMMojoEnvironmentCallbacks(IntPtr mmmojo_env, int type, IntPtr callback)
        => GetFunction<SetMMMojoEnvironmentCallbacksDelegate>("SetMMMojoEnvironmentCallbacks")(mmmojo_env, type, callback);

    public static void SetMMMojoEnvironmentInitParams(IntPtr mmmojo_env, int type, IntPtr param)
        => GetFunction<SetMMMojoEnvironmentInitParamsDelegate>("SetMMMojoEnvironmentInitParams")(mmmojo_env, type, param);

    public static void AppendMMSubProcessSwitchNative(IntPtr mmmojo_env, IntPtr switchStringPtr, IntPtr valuePtr)
        => GetFunction<AppendMMSubProcessSwitchNativeDelegate>("AppendMMSubProcessSwitchNative")(mmmojo_env, switchStringPtr, valuePtr);

    public static void StartMMMojoEnvironment(IntPtr mmmojo_env)
        => GetFunction<StartMMMojoEnvironmentDelegate>("StartMMMojoEnvironment")(mmmojo_env);

    public static void StopMMMojoEnvironment(IntPtr mmmojo_env)
        => GetFunction<StopMMMojoEnvironmentDelegate>("StopMMMojoEnvironment")(mmmojo_env);

    public static void RemoveMMMojoEnvironment(IntPtr mmmojo_env)
        => GetFunction<RemoveMMMojoEnvironmentDelegate>("RemoveMMMojoEnvironment")(mmmojo_env);

    public static IntPtr GetMMMojoReadInfoRequest(IntPtr mmmojo_readinfo, ref uint requestDataSize)
        => GetFunction<GetMMMojoReadInfoRequestDelegate>("GetMMMojoReadInfoRequest")(mmmojo_readinfo, ref requestDataSize);

    public static IntPtr GetMMMojoReadInfoAttach(IntPtr mmmojo_readinfo, ref uint attachDataSize)
        => GetFunction<GetMMMojoReadInfoAttachDelegate>("GetMMMojoReadInfoAttach")(mmmojo_readinfo, ref attachDataSize);

    public static void RemoveMMMojoReadInfo(IntPtr mmmojo_readinfo)
        => GetFunction<RemoveMMMojoReadInfoDelegate>("RemoveMMMojoReadInfo")(mmmojo_readinfo);

    public static int GetMMMojoReadInfoMethod(IntPtr mmmojo_readinfo)
        => GetFunction<GetMMMojoReadInfoMethodDelegate>("GetMMMojoReadInfoMethod")(mmmojo_readinfo);

    public static bool GetMMMojoReadInfoSync(IntPtr mmmojo_readinfo)
        => GetFunction<GetMMMojoReadInfoSyncDelegate>("GetMMMojoReadInfoSync")(mmmojo_readinfo);

    public static IntPtr CreateMMMojoWriteInfo(int method, int sync, uint requestId)
        => GetFunction<CreateMMMojoWriteInfoDelegate>("CreateMMMojoWriteInfo")(method, sync, requestId);

    public static IntPtr GetMMMojoWriteInfoRequest(IntPtr mmmojo_writeinfo, uint requestDataSize)
        => GetFunction<GetMMMojoWriteInfoRequestDelegate>("GetMMMojoWriteInfoRequest")(mmmojo_writeinfo, requestDataSize);

    public static void RemoveMMMojoWriteInfo(IntPtr mmmojo_writeinfo)
        => GetFunction<RemoveMMMojoWriteInfoDelegate>("RemoveMMMojoWriteInfo")(mmmojo_writeinfo);

    public static IntPtr GetMMMojoWriteInfoAttach(IntPtr mmmojo_writeinfo, uint attachDataSize)
        => GetFunction<GetMMMojoWriteInfoAttachDelegate>("GetMMMojoWriteInfoAttach")(mmmojo_writeinfo, attachDataSize);

    public static void SetMMMojoWriteInfoMessagePipe(IntPtr mmmojo_writeinfo, int numOfMessagePipe)
        => GetFunction<SetMMMojoWriteInfoMessagePipeDelegate>("SetMMMojoWriteInfoMessagePipe")(mmmojo_writeinfo, numOfMessagePipe);

    public static void SetMMMojoWriteInfoResponseSync(IntPtr mmmojo_writeinfo, ref IntPtr mmmojo_readinfo)
        => GetFunction<SetMMMojoWriteInfoResponseSyncDelegate>("SetMMMojoWriteInfoResponseSync")(mmmojo_writeinfo, ref mmmojo_readinfo);

    public static bool SendMMMojoWriteInfo(IntPtr mmmojo_env, IntPtr mmmojo_writeinfo)
        => GetFunction<SendMMMojoWriteInfoDelegate>("SendMMMojoWriteInfo")(mmmojo_env, mmmojo_writeinfo);

    public static bool SwapMMMojoWriteInfoCallback(IntPtr mmmojo_writeinfo, IntPtr mmmojo_readinfo)
        => GetFunction<SwapMMMojoWriteInfoCallbackDelegate>("SwapMMMojoWriteInfoCallback")(mmmojo_writeinfo, mmmojo_readinfo);

    public static bool SwapMMMojoWriteInfoMessage(IntPtr mmmojo_writeinfo, IntPtr mmmojo_readinfo)
        => GetFunction<SwapMMMojoWriteInfoMessageDelegate>("SwapMMMojoWriteInfoMessage")(mmmojo_writeinfo, mmmojo_readinfo);
}
