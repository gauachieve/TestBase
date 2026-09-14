namespace TestBase.Shared;

/// <summary>
/// Enkelt, manuelt inkrementert bygg-nummer vist i footeren (ved siden av
/// "(C) [år] PsyTest") slik at brukeren visuelt kan bekrefte at en nettleser
/// faktisk viser den nyeste deployen og ikke en cachet, gammel versjon av
/// siden — økes med 1 for hver commit som skal deployes til psytest.no.
/// </summary>
public static class AppVersjon
{
    public const int Nummer = 5;
}
