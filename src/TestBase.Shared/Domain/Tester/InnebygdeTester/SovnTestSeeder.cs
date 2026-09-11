namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Forenklet søvnutredningsskjema — Haukeland Universitetssykehus (seksjon
/// Søvno). Kilde:
/// https://www.helse-bergen.no/49ec79/siteassets/seksjon/sovno/documents/forenkla-sovnutredningsskjema.pdf
/// (hentet og lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\SOVN.md). STRUKTURELT
/// ulikt de andre innebygde testene: et rent klinisk kartleggingsskjema uten
/// noen offisiell sumskår i kildedokumentet — hver spørsmålsgruppe har sin
/// egen svarskala (se SovnSkaaringsberegner for hvordan dette håndteres i
/// rapporten). Enkelte par-spørsmål i originalen ("Hverdager: ___ / Helg:
/// ___") er her enten slått sammen til ett fritekstfelt som ber om begge
/// verdier, eller splittet i to ledd der et ja/nei-svar egner seg bedre — se
/// kommentarer per ledd. IKKE juridisk/klinisk kvalitetssikret av oss utover
/// dette, se docs/beslutningslogg.md.
/// </summary>
public sealed class SovnTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "sovn";

    private const string Del1Intro = "Har du merket følgende besvær siste 3 måneder?";

    private const string FrekvensSkala = "0:Aldri,1:Sjelden (noen ganger per år),2:Iblant (noen ganger per måned),3:For det meste (flere ganger per uke),4:Alltid (hver dag)";

    /// <summary>De 14 leddene som faktisk telles i RaaSkaar, se SovnSkaaringsberegner — rekkefølge er signifikant.</summary>
    internal static readonly string[] Symptomledd =
    {
        "Vanskelig for å sovne",
        "Gjentatte oppvåkninger med vansker for å sovne igjen",
        "Våknet opp for tidlig (endelig oppvåkning)",
        "For lite søvn (minst 1 time under ditt søvnbehov)",
        "Mareritt",
        "Snorking (ifølge andre)",
        "Pustepauser under søvn (ifølge andre)",
        "Trett/søvnig på arbeid/skole eller i fritiden",
        "Utilsiktede søvnepisoder ('hodet dupper') på arbeid/skole",
        "Utilsiktede søvnepisoder ('hodet dupper') på fritiden",
        "Behov for å kjempe mot søvnen for å holde deg våken",
        "Livlige drømmer ved innsovning",
        "Opplevelse av muskel-lammelse ved oppvåkning",
        "Plutselig tap av muskelkraft (f.eks. 'knekker' i knærne) ved følelsesmessige reaksjoner (som latter, sinne)"
    };

    private const string Kategori = "Søvn og døgnrytme";

    private const string RapportIntroduksjonTekst =
        "Forenklet søvnutredningsskjema er et rent kartleggingsskjema — det gir IKKE en klinisk validert " +
        "sumskår. Rapporten viser en enkel sum av de 14 hovedsymptomene (0–56) og flagger mønstre som kan " +
        "indikere søvnapné eller narkolepsi-lignende symptomer, til støtte for videre klinisk vurdering.";

    public async Task SeedAsync(TestService testService, CancellationToken cancellationToken = default)
    {
        await testService.SikreStandardkategorierAsync(cancellationToken);

        var eksisterende = await testService.HentTestVedKodeAsync(Kode, cancellationToken);
        if (eksisterende is not null)
        {
            await testService.KoblTestTilKategoriAsync(eksisterende.Id, Kategori, cancellationToken);
            await testService.SettRapportIntroduksjonAsync(eksisterende.Id, RapportIntroduksjonTekst, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: "Forenklet søvnutredningsskjema",
            beskrivelse: "Dette skjemaet kartlegger søvnplagene dine de siste tre månedene, samt din vanlige søvnrytme.",
            belonningstekst: "Takk for at du fylte ut søvnskjemaet. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side1 = await testService.LeggTilSideAsync(test.Id, "Søvnsymptomer siste 3 måneder", Del1Intro, cancellationToken);
        foreach (var tekst in Symptomledd)
        {
            await testService.LeggTilLeddAsync(side1.Id, tekst, instruksjon: null, TestSvartype.LikertSkala, FrekvensSkala, cancellationToken);
        }

        await testService.LeggTilLeddAsync(side1.Id, "Har du brukt sovemedisiner på resept, siste 3 måneder?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Har du brukt annen type sovemedisin (ikke på resept), siste 3 måneder?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Har du siste året vært plaget av søvnløshet slik at det har gått ut over arbeidsevnen?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Når går du normalt til sengs (for å sove)? Oppgi både hverdager og helg/ferie.", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Når våkner du normalt opp (endelig oppvåkning)? Oppgi både hverdager og helg/ferie.", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Hvor lenge bruker du å ligge våken før du sovner (i minutter)? Oppgi både hverdager og helg/ferie.", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Hvor mye søvn trenger du (timer og minutter)?", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(
            side1.Id, "Hvor ofte tar du deg en blund på dagtid?", instruksjon: null, TestSvartype.LikertSkala, FrekvensSkala, cancellationToken);
        await testService.LeggTilLeddAsync(side1.Id, "Hvis du tar deg en blund, hvor lenge bruker den å vare (timer og minutter)?", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);

        var side2 = await testService.LeggTilSideAsync(test.Id, "Døgnrytme og søvnkvalitet", instruksjon: null, cancellationToken);
        await testService.LeggTilLeddAsync(
            side2.Id, "Er du morgen- eller kveldsmenneske?", instruksjon: null, TestSvartype.LikertSkala,
            "0:Utpreget morgenmenneske,1:Mer morgen- enn kveldsmenneske,2:Verken eller,3:Mer kvelds- enn morgenmenneske,4:Utpreget kveldsmenneske", cancellationToken);
        await testService.LeggTilLeddAsync(
            side2.Id, "Mener du at du får tilstrekkelig med søvn?", instruksjon: null, TestSvartype.LikertSkala,
            "0:Ja, absolutt tilstrekkelig,1:Ja, stort sett tilstrekkelig,2:Nei, noe utilstrekkelig,3:Nei, klart utilstrekkelig,4:Nei, langt fra tilstrekkelig", cancellationToken);
        await testService.LeggTilLeddAsync(
            side2.Id, "Hvordan synes du at du sover totalt sett?", instruksjon: null, TestSvartype.LikertSkala,
            "0:Veldig bra,1:Ganske bra,2:Verken bra eller dårlig,3:Ganske dårlig,4:Veldig dårlig", cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Har du regelmessig en periode i løpet av dagen hvor du er spesielt trett/søvnig?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Hvis du har en slik periode, når på dagen pleier den å komme?", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(
            side2.Id, "Hvis du blir liggende å døse utover dagen etter en natt med normal søvn, hvordan føler du deg resten av dagen?",
            instruksjon: null, TestSvartype.LikertSkala, "0:Mer opplagt,1:Ingen forskjell,2:Mindre opplagt", cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Bruker du vekkerklokke for å våkne om morgenen på hverdager?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Bruker du vekkerklokke for å våkne om morgenen i helg/ferie?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(
            side2.Id, "Hvis du bruker sovemedisiner, hvor lenge har du brukt slike?", instruksjon: null, TestSvartype.LikertSkala,
            "0:Under en måned,1:1-3 måneder,2:3-12 måneder,3:1-5 år,4:Mer enn 5 år", cancellationToken);
        await testService.LeggTilLeddAsync(
            side2.Id, "Hvis du bruker sovemedisiner, føler du at de hjelper?", instruksjon: null, TestSvartype.LikertSkala,
            "0:Ikke i det hele tatt,1:Ganske dårlig,2:Litt,3:Ganske bra,4:Veldig mye", cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Føler du selv at du har et søvnproblem?", instruksjon: null, TestSvartype.JaNei, null, cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Hvis du har søvnproblemer, hvor lenge har du hatt det (år/måneder)?", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(side2.Id, "Ytterligere kommentarer om søvnen din (valgfritt)", instruksjon: null, TestSvartype.Fritekst, null, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}
