using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WeChatOcr;

public class ImageOcr : IDisposable
{
    private const uint MaxRetryTimes = 99;
    private readonly OcrManager _ocrManager;

    public ImageOcr(string? path = default)
    {
        if (string.IsNullOrEmpty(path))
            _ocrManager = new OcrManager();
        else
            _ocrManager = new OcrManager(path);

        var ocrPtr = GCHandle.ToIntPtr(GCHandle.Alloc(_ocrManager));
        _ocrManager = (GCHandle.FromIntPtr(ocrPtr).Target as OcrManager)!;
        _ocrManager.StartWeChatOcr(ocrPtr);
    }

    public void Dispose()
    {
        _ocrManager.Dispose();
    }

    public void Run(string imagePath, Action<string, WeChatOcrResult?>? callback)
    {
        if (callback != null) _ocrManager.SetOcrResultCallback(callback);
        var retryCount = 0;
        while (retryCount <= MaxRetryTimes)
            try
            {
                _ocrManager.DoOcrTask(imagePath);
                return;
            }
            catch (OverflowException)
            {
                Thread.Sleep(10);
                retryCount++;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }
    }

    public void Run(byte[] bytes, Action<string, WeChatOcrResult?>? callback, ImageType imgType = ImageType.Png)
    {
        var imgPath = Path.Combine(Path.GetTempPath(), $"generate4wechat.{imgType.ToString().ToLower()}");
        Utilities.WriteBytesToFile(imgPath, bytes);
        Run(imgPath, callback);
    }
}

public enum ImageType
{
    Png,
    Jpeg,
    Bmp
}