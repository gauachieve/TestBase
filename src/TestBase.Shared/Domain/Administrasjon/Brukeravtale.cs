namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Bruker- og lisensavtalen behandlere må godta for å registrere seg, jf.
/// kravdokumentets Del 3. IKKE juridisk rådgivning — et utkast bygget på
/// vanlige mønstre for slike avtaler (formål, databehandling, taushetsplikt,
/// bruksvilkår, endringshåndtering), på samme måte som
/// docs/compliance-dpia-utkast.md. Bør kvalitetssikres av jurist/DPO før den
/// brukes med reelle behandlere. Øk <see cref="GjeldendeVersjon"/> når
/// <see cref="Tekst"/> endres — det tvinger alle behandlere til å re-godkjenne
/// ved neste innlogging (se BehandlerAuthenticationService).
/// </summary>
public static class Brukeravtale
{
    public const int GjeldendeVersjon = 1;

    public const string Tekst = """
        Bruker- og lisensavtale for behandlere i TestBase (versjon 1)

        1. Formål
        Denne avtalen regulerer din bruk av TestBase som behandler (godkjent helsepersonell) til
        å administrere pasienter, tildele psykologiske tester og motta resultater/rapporter.

        2. Behandling av helse- og personopplysninger
        Du behandler helseopplysninger om dine pasienter i egenskap av helsepersonell, med eget
        selvstendig behandlingsansvar for din journalføring, i tråd med helsepersonelloven,
        pasientjournalloven, GDPR og Normen for informasjonssikkerhet i helse- og
        omsorgssektoren. TestBase er databehandler/underleverandør for lagring og formidling av
        testresultater på dine vegne, ikke selvstendig behandlingsansvarlig for pasientdataene.

        3. Taushetsplikt
        Du er underlagt lovbestemt taushetsplikt for alle pasientopplysninger du får tilgang til
        gjennom systemet, på samme måte som i din øvrige kliniske virksomhet.

        4. HPR-nummer og godkjenning
        Ditt HPR-nummer kontrolleres mot Helsepersonellregisteret av en systemadministrator. Du
        har en prøveperiode på 7 dager fra fullført registrering hvor systemet er fullt
        tilgjengelig; etter dette kreves godkjent HPR-nummer for å legge til nye pasienter.

        5. Bruksvilkår
        Du skal kun bruke systemet til lovlig helsefaglig virksomhet, ikke dele egen
        pålogging med andre, og varsle administrator ved mistanke om uautorisert tilgang til
        din konto.

        6. Endringer i avtalen
        Endres denne avtalen, må du godta den nye versjonen på nytt før du kan fortsette å bruke
        systemet.

        7. Oppsigelse
        Administrator kan fryse eller avslutte din tilgang ved brudd på denne avtalen eller
        gjeldende lovgivning. Du kan når som helst be om at din konto arkiveres.
        """;
}
