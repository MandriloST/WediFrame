using QRCoder;

namespace WediFrame.Modules.Events.Services;

public interface IQrCodeService
{
    /// <summary>QR code as PNG bytes. pixelsPerModule controls output size (print quality).</summary>
    byte[] CreatePng(string content, int pixelsPerModule);

    /// <summary>QR code as an SVG string — vector, ideal for print (table cards, invitations).</summary>
    string CreateSvg(string content, int pixelsPerModule);
}

/// <summary>
/// QRCoder-based implementation. ECC level Q (25% recovery) — QR codes on wedding
/// tables get splashed, folded and photographed at angles; extra redundancy is cheap.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    public byte[] CreatePng(string content, int pixelsPerModule)
    {
        using var data = QRCodeGenerator.GenerateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }

    public string CreateSvg(string content, int pixelsPerModule)
    {
        using var data = QRCodeGenerator.GenerateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var svg = new SvgQRCode(data);
        return svg.GetGraphic(pixelsPerModule);
    }
}
