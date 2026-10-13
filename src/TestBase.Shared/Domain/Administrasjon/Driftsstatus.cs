namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>Grønn = fungerer. Gul = fungerer, men nær en terskel verdt å følge med på (f.eks. lav SMS-kreditt). Rød = faktisk feil/nede. Grå = ikke konfigurert i dette miljøet (f.eks. lokal dev uten ekte nøkler) — IKKE det samme som rødt, siden mock-modus er en bevisst, fungerende tilstand lokalt.</summary>
public enum DriftsstatusLys
{
    Gronn,
    Gul,
    Rod,
    Graa
}

/// <summary><see cref="VarighetMs"/> er null for sjekker som ikke gjør noe eksternt kall (f.eks. en ren konfigurasjonssjekk).</summary>
public sealed record DriftsstatusSjekk(string Navn, DriftsstatusLys Lys, string Detalj, long? VarighetMs);
