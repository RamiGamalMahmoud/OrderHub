using QRCoder;
using System.IO;
using System.Windows.Media.Imaging;

namespace OrderHub.UI.Helpers;
public static class QrCodeGenerator
{
    public static BitmapImage Generate(string content)
    {
        using QRCodeGenerator generator = new();

        using QRCodeData data = generator.CreateQrCode(
            content,
            QRCodeGenerator.ECCLevel.M);

        PngByteQRCode qrCode = new(data);

        byte[] bytes = qrCode.GetGraphic(50);

        using MemoryStream stream = new(bytes);

        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();

        return image;
    }
}