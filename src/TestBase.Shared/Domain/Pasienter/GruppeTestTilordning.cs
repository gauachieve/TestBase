namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Én test som automatisk skal tildeles enhver pasient som melder seg inn i
/// gruppen (via gruppens QR-kode/lenke, se fase 2) — satt ved gruppeopprettelse,
/// men redigerbar senere. Selve utsendingen skjer via den eksisterende
/// TestTildelingsService, samme betalingsgate som en manuell tildeling
/// (se docs/beslutningslogg.md "Invitasjons- og gruppesystem").
/// </summary>
public sealed class GruppeTestTilordning
{
    public long Id { get; set; }
    public long GruppeId { get; set; }
    public long TestId { get; set; }
    public DateTimeOffset OpprettetUtc { get; set; }
}
