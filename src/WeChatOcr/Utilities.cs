using Microsoft.Win32;

namespace WeChatOcr;

public partial class Utilities
{
    /// <summary>
    ///     微信默认安装目录
    /// </summary>
    private const string WeChatDefaultPath = @"C:\Program Files\Tencent\WeChat";

    private const string MmMojoDll = "mmmojo.dll";
    private const string MmMojo64Dll = "mmmojo_64.dll";

    /// <summary>
    ///     获取微信安装目录
    /// </summary>
    /// <param name="path">
    ///     C:\Program Files\Tencent\WeChat
    ///     or
    ///     C:\Program Files\Tencent\WeChat\[3.9.12.11]
    /// </param>
    /// <returns></returns>
    public static string? GetWeChatDir(string? path = default)
    {
        path ??= WeChatDefaultPath;

        // 根据给定路径下 mmmojo.dll 的存在与否，判断是否为微信完整路径
        if (File.Exists(Path.Combine(path, MmMojoDll)))
            return path.TrimEnd('\\');

        // 从注册表中获取微信安装版本并根据参数path拼接微信安装目录
        using var key =
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\WeChat");
        if (key?.GetValue("DisplayVersion") is not string displayVersion) return default;
        var wechatDir = Path.Combine(path, "[" + displayVersion + "]");
        return Directory.Exists(wechatDir) ? wechatDir : default;
    }

    /// <summary>
    ///     获取微信OCR可执行文件路径
    /// </summary>
    /// <returns></returns>
    public static string? GetWeChatOcrExePath()
    {
        var searchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Tencent\WeChat\XPlugin\Plugins\WeChatOCR");
        var ocrExePath = Directory.EnumerateFiles(searchPath, "WeChatOCR.exe", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (ocrExePath != null && File.Exists(ocrExePath)) return ocrExePath;
        return default;
    }

    public static void WriteBytesToFile(string filePath, byte[] bytes)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        fileStream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    ///     复制微信安装目录下的mmmojo.dll、mmmojo_64.dll到软件目录
    /// </summary>
    /// <param name="wechatFullDir">
    ///     微信完整路径包含版本号
    ///     * C:\Program Files\Tencent\WeChat\[3.9.12.11]
    /// </param>
    public static void CopyMmmojoDll(string wechatFullDir)
    {
        if (!Directory.Exists(Constant.WeChatOcrData)) Directory.CreateDirectory(Constant.WeChatOcrData);
        var mmMojoFullPath = Path.Combine(wechatFullDir, MmMojoDll);
        var mmMojo64FullPath = Path.Combine(wechatFullDir, MmMojo64Dll);
        var targetMmMojoFullPath = Path.Combine(Constant.WeChatOcrData, MmMojoDll);
        var targetMmMojo64FullPath = Path.Combine(Constant.WeChatOcrData, MmMojo64Dll);
        if (!File.Exists(targetMmMojoFullPath))
            File.Copy(mmMojoFullPath, targetMmMojoFullPath);
        if (!File.Exists(targetMmMojo64FullPath))
            File.Copy(mmMojo64FullPath, targetMmMojo64FullPath);
    }

    /// <summary>
    ///     移除软件目录下的mmmojo.dll、mmmojo_64.dll
    /// </summary>
    /// <param name="error"></param>
    public static bool RemoveMmmojoDll(out string error)
    {
        var ret = true;
        error = string.Empty;
        var targetPath = AppDomain.CurrentDomain.BaseDirectory;
        var targetMmMojoFullPath = Path.Combine(targetPath, MmMojoDll);
        var targetMmMojo64FullPath = Path.Combine(targetPath, MmMojo64Dll);
        try
        {
            if (File.Exists(targetMmMojoFullPath))
                File.Delete(targetMmMojoFullPath);
            if (File.Exists(targetMmMojo64FullPath))
                File.Delete(targetMmMojo64FullPath);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            ret = false;
        }

        return ret;
    }
}