namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Multidimensional Psychological Flexibility Inventory, kortversjon (MPFI-24) — Rolffs, Rogge
/// &amp; Wilson (2018), Hexaflex-modellen (psykologisk fleksibilitet/rigiditet, ACT). 24 ledd,
/// 12 delskalaer à 2 ledd (6 FLEKSIBILITET: Aksept/Nærvær/Selvet/Defusjon/Verdier/Handling, 6
/// RIGIDITET: Opplevelsesunngåelse/Ikke-til-stede/Begrepsselv/Fusjon/Manglende-kontakt-verdier/
/// Passivitet), 6-punkts Likert-skala (Stemte aldri–Stemte alltid).
///
/// KILDE OG LISENS: itemtekst er den OFFISIELLE norske oversettelsen (bruker-levert PDF,
/// "Norsk MPFI-24 kortversjon"), oversatt/tilpasset av psykologspesialist Asle Thude Elen,
/// psykologspesialist Hans J. Johansen og psykolog/phd. Peter Lyby (redaksjon), ETTER TILLATELSE
/// fra opphavsmann Ronald Rogge (31.01.20), med økonomisk støtte fra Stiftelsen CatoSenteret —
/// samme "bruk offisiell oversettelse verbatim"-mønster som WHO-5/PHQ-9/MADRS-S (hentet fra
/// Helsebiblioteket), IKKE samme "egenforfattet parafrasering"-mønster som EDE-Q/TRAPS II/SCID-5-PF
/// (der ingen slik tillatelse forelå). Originalpublikasjon: Rolffs JL, Rogge RD, Wilson KG.
/// Disentangling Components of Flexibility via the Hexaflex Model: Development and Validation of
/// the Multidimensional Psychological Flexibility Inventory (MPFI). Assessment. 2018;25(4):458-482.
/// doi:10.1177/1073191116645905.
///
/// KJØNNSSPESIFIKK NORMERING: normtabellen (bruker-levert regneark, "MPFI_24 skåring.xlsx") gir
/// EGNE norm-gjennomsnitt for menn/kvinner per delskala — se Mpfi24Skaaringsberegner. Krever derfor
/// Pasient.BiologiskKjonnVedFodsel satt (Test.KreverBiologiskKjonn, håndhevet i
/// TestTildelingsService.TildelOgVarsleAsync FØR tildeling i det hele tatt skjer).
///
/// REELL FEIL FUNNET OG RETTET i kildearket under bygging: regnearkets "Fusjon"-delskåre-formel
/// (AVERAGE(C30:C32)) inkluderte ved en drafeil ledd 21 i tillegg til de tiltenkte leddene 19-20
/// (radens EGEN etikett sier eksplisitt "ledd 19 og 20", og ledd 21 brukes allerede, korrekt, i
/// NESTE rad "Mangel på kontakt med verdier ledd 21 og 22") — et klassisk drag-fill-en-rad-for-
/// langt-mønster. Rettet her til kun ledd 19-20, bekreftet mot den kjente MPFI-hexaflex-
/// itemstrukturen i originalpublikasjonen (ikke reprodusert regnearkets feil).
/// </summary>
public sealed class Mpfi24TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "mpfi_24";

    private const string Skala = "1:Stemte aldri,2:Stemte sjeldent,3:Stemte av og til,4:Stemte ofte,5:Stemte veldig ofte,6:Stemte alltid";

    private static readonly string[] Ledd =
    {
        "Var jeg åpen for å observere ubehagelige tanker og følelser uten å gjøre noe med dem.",
        "Prøvde jeg å falle til ro med mine negative tanker og følelser heller enn å kjempe mot dem.",
        "Var jeg oppmerksom på og bevisst følelsene mine.",
        "Var jeg jevnlig oppmerksom på tankene og følelsene mine.",
        "Selv når jeg følte meg såret eller opprørt, prøvde jeg å innta et større perspektiv.",
        "Hjalp jeg meg selv gjennom tøffe øyeblikk ved å se på livet mitt fra et større perspektiv.",
        "Klarte jeg å la negative følelser komme og gå, uten å bli hektet opp i dem.",
        "Når jeg var opprørt, klarte jeg å la de negative følelsene komme og gå uten å bli opphengt i dem.",
        "Var jeg i nær kontakt med hva som er viktig for meg og mitt liv.",
        "Holdt jeg fast ved mine viktigste prioriteringer i livet.",
        "Selv om jeg snublet i mine forsøk, sluttet jeg ikke å jobbe mot det som er viktig.",
        "Selv i tøffe tider klarte jeg å ta skritt i retning av det jeg verdsetter i livet.",
        "Når jeg opplevde et vondt minne, prøvde jeg å distrahere meg selv for å få det vekk.",
        "Prøvde jeg å distrahere meg selv når jeg hadde ubehagelige følelser.",
        "Gjorde jeg de fleste ting på autopilot, med lite oppmerksomhet på hva jeg gjorde.",
        "Gjorde jeg de fleste ting uten å være mentalt til stede.",
        "Tenkte jeg at noen av følelsene mine var dårlige eller upassende og at jeg ikke burde ha dem.",
        "Kritiserte jeg meg selv for å ha irrasjonelle eller upassende følelser.",
        "Hang negative tanker og følelser igjen hos meg i lang tid.",
        "Hadde plagsomme tanker en tendens til å gjenta seg i hodet mitt, som om det var hakk i plata.",
        "Mistet jeg ofte kontakten med det som er viktigst for meg i dagliglivet.",
        "Når livet ble hektisk, mistet jeg ofte kontakten med de tingene jeg verdsetter.",
        "Fanget ofte negative følelser meg inn i passivitet.",
        "Stanset negative følelser lett mine planer."
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "MPFI-24 (Multidimensional Psychological Flexibility Inventory, kortversjon) måler " +
        "psykologisk fleksibilitet og rigiditet etter Hexaflex-modellen (ACT), 12 delskalaer à 2 " +
        "ledd. Delskåre og globalskåre er gjennomsnitt (skala 1-6), sammenlignet mot kjønnsspesifikke " +
        "normtall. Offisiell norsk oversettelse (Elen, Johansen, Lyby, etter tillatelse fra Ronald " +
        "Rogge), originalpublikasjon Rolffs, Rogge & Wilson (2018), Assessment 25(4):458-482.";

    public async Task SeedAsync(TestService testService, CancellationToken cancellationToken = default)
    {
        await testService.SikreStandardkategorierAsync(cancellationToken);

        var eksisterende = await testService.HentTestVedKodeAsync(Kode, cancellationToken);
        if (eksisterende is not null)
        {
            await testService.KoblTestTilKategoriAsync(eksisterende.Id, Kategori, cancellationToken);
            await testService.SettRapportIntroduksjonAsync(eksisterende.Id, RapportIntroduksjonTekst, cancellationToken);
            await testService.SettKreverBiologiskKjonnAsync(eksisterende.Id, true, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: "MPFI-24 (Psykologisk fleksibilitet)",
            beskrivelse: "Nedenfor finner du 24 utsagn om hvordan du forholdt deg til opplevelser du kan ha hatt de to siste ukene. Dersom du ikke har opplevd det som spørres om, svar så godt du kan ut fra hvordan du tror du vanligvis ville ha forholdt deg til dette.",
            belonningstekst: "Takk for at du fylte ut MPFI-24.",
            kode: Kode,
            cancellationToken: cancellationToken);
        await testService.SettKreverBiologiskKjonnAsync(test.Id, true, cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Gjennom de to siste ukene …", null, cancellationToken);
        foreach (var spørsmål in Ledd)
        {
            await testService.LeggTilLeddAsync(side.Id, spørsmål, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
