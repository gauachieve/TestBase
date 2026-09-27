namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Resultatet av å skåre én fullført TestTildeling. <paramref name="Indikatorer"/>
/// er valgfrie, navngitte kategoriske konklusjoner utover selve tallskåren
/// (se TestSkaaringIndikator) — null/tom for tester som ikke har noen (de
/// fleste), populert av f.eks. Who5Skaaringsberegner.
/// <paramref name="GyldighetsAdvarsel"/> (2026-09-23) er satt av
/// TestService.BeregnSkaaringAsync — IKKE av den enkelte skåringsberegner —
/// når andelen ubesvarte ledd overskrider testens Test.MaksUbesvartProsent.
/// Null (vanligste tilfelle) betyr ingen advarsel. Se
/// docs/beslutningslogg.md "Normert gjennomsnitt-imputering + gyldighetsgrense".
/// <paramref name="SkjulProsent"/> (2026-09-27) lar en skåringsberegner be
/// individrapporten (Rapport.cshtml, begge Areas) om å SKJULE prosent- og
/// råskår-visningen — for tester der en sum på tvers av usammenlignbare
/// deltester (f.eks. M.I.N.I.-strukturdemoens 10 uavhengige moduler) ikke gir
/// noen meningsfull enkelt-prosent. Standard usann for alle eksisterende
/// tester (ingen kodeendring nødvendig andre steder).
/// </summary>
public sealed record TestSkaaring(
    int RaaSkaar, int RaaSkaarMaks, int ProsentSkaar, string Fortolkning,
    IReadOnlyList<TestSkaaringIndikator>? Indikatorer = null,
    string? GyldighetsAdvarsel = null,
    bool SkjulProsent = false);
