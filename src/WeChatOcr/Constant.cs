namespace WeChatOcr;

public static class Constant
{
    /// <summary>
    ///     内部OCR数据目录（绝对路径）
    /// </summary>
    public static readonly string WeChatOcrData = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wco_data");

#if WIN32
    public static readonly string MojoDllName = Path.Combine(WeChatOcrData, "mmmojo.dll");
#else
    public static readonly string MojoDllName = Path.Combine(WeChatOcrData, "mmmojo_64.dll");
#endif
}
