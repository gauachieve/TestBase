namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Hvilke kanaler pasienten ønsker varsel om nye testtildelinger på, valgt
/// ved egenregistrering (jf. PasientInvitasjonService.FullforRegistreringAsync).
/// Standard er <see cref="Begge"/>. Uavhengig av dette sendes det aldri på en
/// kanal pasienten mangler kontaktinfo for (se TestTildelingsService).
/// </summary>
public enum Varslingspreferanse
{
    Sms,
    Epost,
    Begge
}
