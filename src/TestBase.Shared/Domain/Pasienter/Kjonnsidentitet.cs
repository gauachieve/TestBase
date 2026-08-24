namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Valgfritt felt ved pasientregistrering (IKKE påkrevd, i motsetning til
/// <see cref="BiologiskKjonn"/>) — jf. Del 4 i kravdokumentet: "kjønn (støtte
/// også «annet» og «spesifiser»)". Se <see cref="Pasient.KjonnsidentitetSpesifisert"/>.
/// </summary>
public enum Kjonnsidentitet
{
    Mann,
    Kvinne,
    Annet
}
