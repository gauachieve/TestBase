using QRCoder;

namespace TestBase.Web.Security;

/// <summary>
/// Genererer QR-kode-bilder for behandler-/gruppeinvitasjoner (se
/// GruppeService og docs/beslutningslogg.md "Invitasjons- og gruppesystem,
/// fase 2"). Bruker BEVISST QRCoder sin rene, administrerte
/// <see cref="PngByteQRCode"/> — IKKE <c>QRCode</c>-klassen, som krever
/// System.Drawing.Common/GDI+ og ikke fungerer pålitelig på Linux-baserte
/// Azure App Service-containere (som denne appen kjører på).
/// </summary>
public static class QrBildeGenerator
{
    public static byte[] GenererPng(string innhold, int pikslerPerModul = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(innhold, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(pikslerPerModul);
    }
}
