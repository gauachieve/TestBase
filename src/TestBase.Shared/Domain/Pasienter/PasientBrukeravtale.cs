namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Bruker- og samtykkeavtalen pasienter må godta for å registrere seg, jf.
/// Del 4 i kravdokumentet. IKKE juridisk rådgivning — et utkast bygget på
/// vanlige mønstre for slike avtaler, på samme måte som
/// docs/compliance-dpia-utkast.md og Brukeravtale (for behandlere). Bør
/// kvalitetssikres av jurist/DPO før den brukes med reelle pasienter. Øk
/// <see cref="GjeldendeVersjon"/> når <see cref="Tekst"/> endres.
/// </summary>
public static class PasientBrukeravtale
{
    public const int GjeldendeVersjon = 1;

    public const string Tekst = """
        Bruker- og samtykkeavtale for pasienter i TestBase (versjon 1)

        1. Formål
        Denne avtalen regulerer din bruk av TestBase til å svare på psykologiske tester som din
        behandler har tildelt deg, og til lagring av svarene og resultatene dine.

        2. Behandling av helseopplysninger
        Svarene dine regnes som helseopplysninger og behandles i tråd med pasientjournalloven,
        helsepersonelloven, GDPR og Normen for informasjonssikkerhet i helse- og
        omsorgssektoren. Din behandler er behandlingsansvarlig for opplysningene; TestBase er
        databehandler for lagring og formidling på behandlerens vegne.

        3. Dine rettigheter
        Du har rett til innsyn i egne opplysninger, og til å be om retting eller sletting, med de
        begrensningene som følger av helselovgivningens krav til journalføring. Opplysningene
        slettes automatisk senest 10 år etter at pasientforholdet er avsluttet, med mindre lov
        krever lengre oppbevaring.

        4. Betaling
        Enkelte tester kan ha en kostnad du selv må dekke for å kunne fylle dem ut. Betaling
        skjer i så fall via Vipps. Du varsles om prisen før du blir bedt om å betale, og testen
        gjennomføres først etter bekreftet betaling.

        5. Samtykke
        Ved å krysse av nedenfor samtykker du til lagring av opplysningene dine som beskrevet
        over, og til at du kan bli bedt om å betale en mindre sum via Vipps for å fylle ut enkelte
        tester.

        6. Endringer i avtalen
        Endres denne avtalen, må du godta den nye versjonen på nytt før du kan fortsette å bruke
        systemet.
        """;
}
