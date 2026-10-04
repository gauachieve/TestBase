namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Én "drop" i et Behandlingsprogram — en gjenbrukbar tidsplan ("så vi kan gjenbruke dem", jf.
/// kravet) uttrykt som et ANTALL DAGER ETTER FORRIGE drop (0 for den første, som i stedet
/// forankres til Behandlingsprogram.StartUkedag/StartKlokkeslett), pluss et fra-til-tidsvindu
/// samme dag som drop-tidspunktet faktisk randomiseres innenfor. Se ProgramService sin
/// XML-doc for selve antagelsen om hvorfor dag-offset ble valgt fremfor en egen ukedag per
/// drop (kravet er underspesifisert utover selve starttidspunktet).
/// </summary>
public sealed class ProgramDrop
{
    public long Id { get; set; }
    public long ProgramId { get; set; }
    public int Rekkefolge { get; set; }

    /// <summary>0 for første drop (fyrer samme dag som Behandlingsprogram sin beregnede starttid) — ellers antall dager etter FORRIGE drops fyringsdag.</summary>
    public int DagerEtterForrige { get; set; }

    public TimeSpan FraKlokkeslett { get; set; }
    public TimeSpan TilKlokkeslett { get; set; }

    /// <summary>Standard PÅ (brukerens eksplisitte krav) — se ProgramService.RandomiserTidspunkt for selve nattdefinisjonen.</summary>
    public bool UnngaaNatt { get; set; } = true;
}
