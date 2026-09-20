targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Navn på azd-miljøet (brukes til å avlede ressursnavn)')
param environmentName string

@minLength(1)
@description('Primær Azure-region for alle ressurser')
param location string

@description('Administrator-brukernavn for MySQL Flexible Server')
param mysqlAdministratorLogin string = 'testbaseadmin'

@description('Delt nøkkel for StagingGate (se Security/StagingGate.cs) — tom verdi deaktiverer sperren. Settes via azd-miljøvariabelen STAGING_GATE_ACCESS_KEY, ALDRI som literal her (unngår at nøkkelen havner i kildekontroll).')
@secure()
param stagingGateAccessKey string = ''

@description('SMS-avsendernavn (f.eks. "PsyTest") — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen SMS_SENDER_ID (se docs/beslutningslogg.md "SMS-integrasjon").')
param smsSenderId string = ''

@description('Vonage API-nøkkel for SMS — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen VONAGE_API_KEY.')
@secure()
param vonageApiKey string = ''

@description('Vonage API-hemmelighet for SMS — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen VONAGE_API_SECRET, ALDRI som literal her.')
@secure()
param vonageApiSecret string = ''

@description('Idura (BankID-testintegrasjon) OIDC Authority, f.eks. https://psytest.test.idura.broker — tom verdi deaktiverer BankID-testintegrasjonen (se docs/beslutningslogg.md).')
param bankIdIduraAuthority string = ''

@description('Idura Client ID for BankID-testintegrasjonen — ikke hemmelig, men settes via azd-miljøvariabel for konsistens.')
param bankIdIduraClientId string = ''

@description('Idura Client Secret for BankID-testintegrasjonen — settes via azd-miljøvariabelen BANKID_IDURA_CLIENT_SECRET, ALDRI som literal her.')
@secure()
param bankIdIduraClientSecret string = ''

@description('EKTE Idura BankID-produksjonstenant sin OIDC Authority (f.eks. https://psytest-no.idura.broker) — KUN satt på live. Program.cs foretrekker denne for "BankIdInnlogging"-schemaet når den er satt, faller ellers tilbake til test-tenanten over (beta/lokal dev bruker alltid test-tenanten, ingen egen flagg nødvendig). Tom verdi = fortsatt test-tenanten. Settes via azd-miljøvariabelen BANKID_IDURA_PRODUKSJON_AUTHORITY.')
param bankIdIduraProduksjonAuthority string = ''

@description('EKTE Idura BankID-produksjonstenant sin Client ID — ikke hemmelig, men settes via azd-miljøvariabel for konsistens. Settes via BANKID_IDURA_PRODUKSJON_CLIENT_ID.')
param bankIdIduraProduksjonClientId string = ''

@description('EKTE Idura BankID-produksjonstenant sin Client Secret — settes via azd-miljøvariabelen BANKID_IDURA_PRODUKSJON_CLIENT_SECRET, ALDRI som literal her.')
@secure()
param bankIdIduraProduksjonClientSecret string = ''

@description('Ekte personnummer for en seedet administrator-konto (IKKE syntetisk testdata) — tom verdi deaktiverer seedingen. Settes via azd-miljøvariabelen SEED_ADMIN_PERSONNUMMER, ALDRI som literal her (se docs/beslutningslogg.md "Seed av brukerens egen admin-konto").')
@secure()
param seedAdminPersonnummer string = ''

@description('Fullt navn for den seedede administrator-kontoen. Settes via azd-miljøvariabelen SEED_ADMIN_NAVN, ALDRI som literal her.')
@secure()
param seedAdminNavn string = ''

@description('Mobilnummer (for ekte SMS-2FA) for den seedede administrator-kontoen. Settes via azd-miljøvariabelen SEED_ADMIN_MOBILNR, ALDRI som literal her.')
@secure()
param seedAdminMobilNr string = ''

@description('E-post (for ekte e-post-2FA) for den seedede administrator-kontoen. Settes via azd-miljøvariabelen SEED_ADMIN_EPOST, ALDRI som literal her.')
@secure()
param seedAdminEpost string = ''

@description('Vipps ePayment API Client ID — tom verdi gir MockVippsClient. Settes via azd-miljøvariabelen VIPPS_CLIENT_ID (se docs/beslutningslogg.md "Vipps + Stripe (Apple Pay/Google Pay)").')
@secure()
param vippsClientId string = ''

@description('Vipps ePayment API Client Secret — settes via azd-miljøvariabelen VIPPS_CLIENT_SECRET, ALDRI som literal her.')
@secure()
param vippsClientSecret string = ''

@description('Vipps Ocp-Apim-Subscription-Key — settes via azd-miljøvariabelen VIPPS_SUBSCRIPTION_KEY, ALDRI som literal her.')
@secure()
param vippsSubscriptionKey string = ''

@description('Vipps Merchant Serial Number (MSN) — ikke hemmelig, men settes via azd-miljøvariabel for konsistens.')
param vippsMerchantSerialNumber string = ''

@description('Vipps-miljø: "Test" (standard, mot apitest.vipps.no) eller "Produksjon" (mot api.vipps.no) — ikke hemmelig. Settes via azd-miljøvariabelen VIPPS_MILJO.')
param vippsMiljo string = 'Test'

@description('KUN beta-miljøet: skrur på runtime-bryteren for Vipps/Stripe Mock/Test/Produksjon på Admin/MinSide, se docs/beslutningslogg.md "Beta-miljø". "true"/"false" som STRENG, ikke bool — azd sin parameter-substitusjon skjer inni en JSON-strengverdi, se resources.bicep. Settes via azd-miljøvariabelen MILJO_ER_BETA. IKKE sett til "true" på live/test.')
param erBeta string = 'false'

@description('Skrur på EKTE Idura BankID for admin/behandler sin FELLES innloggingsside (Pages/Konto/LoggInn) — BEVISST ET EGET FLAGG, ikke erBeta (de to styrer urelaterte ting, se Program.cs). Trygt å sette "true" på live: påvirker KUN hvilken IdP admin/behandler-login bruker, ALDRI betalingsmodus. "true"/"false" som streng. Settes via azd-miljøvariabelen MILJO_EKT_BANKID_PROFESJONELL.')
param ektBankIdProfesjonell string = 'false'

@description('KUN beta: Vipps sin EGEN test-/MT-merchant Client ID (separat fra vippsClientId over, som på beta gjenbrukes som "Produksjon" — samme ekte konto som live). Settes via azd-miljøvariabelen VIPPS_BETATEST_CLIENT_ID.')
@secure()
param vippsBetaTestClientId string = ''

@description('KUN beta: Vipps test-merchant Client Secret — settes via azd-miljøvariabelen VIPPS_BETATEST_CLIENT_SECRET, ALDRI som literal her.')
@secure()
param vippsBetaTestClientSecret string = ''

@description('KUN beta: Vipps test-merchant Ocp-Apim-Subscription-Key — settes via azd-miljøvariabelen VIPPS_BETATEST_SUBSCRIPTION_KEY, ALDRI som literal her.')
@secure()
param vippsBetaTestSubscriptionKey string = ''

@description('KUN beta: Vipps test-merchant Merchant Serial Number — ikke hemmelig, men settes via azd-miljøvariabel for konsistens (VIPPS_BETATEST_MERCHANT_SERIAL_NUMBER).')
param vippsBetaTestMerchantSerialNumber string = ''

@description('Hemmelighet for å verifisere Vipps sine webhook-forespørsler (fra webhook-registreringen, se Security/PaymentWebhooks.cs) — settes via azd-miljøvariabelen VIPPS_WEBHOOK_SECRET, ALDRI som literal her.')
@secure()
param vippsWebhookSecret string = ''

@description('Stripe secret key (kort/Apple Pay/Google Pay) — tom verdi gir MockStripeClient. Settes via azd-miljøvariabelen STRIPE_SECRET_KEY, ALDRI som literal her.')
@secure()
param stripeSecretKey string = ''

@description('Stripe publishable key — ikke hemmelig (brukes i nettleseren), men settes via azd-miljøvariabel for konsistens.')
param stripePublishableKey string = ''

@description('Hemmelighet for å verifisere Stripe sine webhook-forespørsler (fra Stripe Dashboard) — settes via azd-miljøvariabelen STRIPE_WEBHOOK_SECRET, ALDRI som literal her.')
@secure()
param stripeWebhookSecret string = ''

@description('Midlertidig HTTP Basic Auth-brukernavn for StagingGate, kun til automatiserte tredjeparts nettsted-verifiseringer (f.eks. Vipps sin merchant-registrering) — tom verdi deaktiverer det. Settes via azd-miljøvariabelen STAGING_GATE_BASIC_AUTH_USERNAME, fjernes igjen når verifiseringen er fullført (se docs/beslutningslogg.md).')
@secure()
param stagingGateBasicAuthUsername string = ''

@description('Midlertidig HTTP Basic Auth-passord for StagingGate — settes via azd-miljøvariabelen STAGING_GATE_BASIC_AUTH_PASSWORD, ALDRI som literal her.')
@secure()
param stagingGateBasicAuthPassword string = ''

@description('Offentlig base-URL for lenker fra bakgrunnstjenester (SMS/e-post) — tom verdi faller tilbake til appens *.azurewebsites.net-vertsnavn. Settes via azd-miljøvariabelen VARSLING_BASE_URL, f.eks. https://www.psytest.no for live eller https://beta.psytest.no for beta.')
param varslingBaseUrl string = ''

@description('Skrur på AUTH-RELATERTE utviklingssnarveier (PersonnummerOverride-bypass, 2FA-kode vist i klartekst, AdminId+passord-innlogging, diagnostiske BankID-/betalingstestsider) — BEVISST ET EGET FLAGG, ikke ASPNETCORE_ENVIRONMENT/IsDevelopment(), siden live kjører "Development" KUN for automatisk databasemigrering ved oppstart og ALDRI skal ha disse snarveiene aktive (se docs/beslutningslogg.md "StagingGate fjernet fra live"). "true" lokalt og på beta, "false" på live. "true"/"false" som STRENG, ikke bool. Settes via azd-miljøvariabelen MILJO_TILLAT_UTVIKLINGSSNARVEIER.')
param tillatUtviklingsSnarveier string = 'false'

@description('Snevrere enn tillatUtviklingsSnarveier over — styrer KUN PersonnummerOverride (admin/behandler+pasient-innlogging). MIDLERTIDIG "true" på live fra 2026-09-20 (se docs/beslutningslogg.md "PersonnummerOverride midlertidig gjeninnført på live") siden MockBankIdProvider er den ENESTE IBankIdProvider som noensinne registreres, og uten denne kan INGEN ekte bruker logge inn før Miljo:EktBankIdProfesjonell er skrudd på. Fjern/sett "false" igjen så snart ekte BankID er verifisert på live. "true"/"false" som STRENG. Settes via azd-miljøvariabelen MILJO_TILLAT_PERSONNUMMER_OVERRIDE.')
param tillatPersonnummerOverride string = 'false'

var resourceToken = uniqueString(subscription().id, environmentName, location)
var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
    mysqlAdministratorLogin: mysqlAdministratorLogin
    stagingGateAccessKey: stagingGateAccessKey
    smsSenderId: smsSenderId
    vonageApiKey: vonageApiKey
    vonageApiSecret: vonageApiSecret
    bankIdIduraAuthority: bankIdIduraAuthority
    bankIdIduraClientId: bankIdIduraClientId
    bankIdIduraClientSecret: bankIdIduraClientSecret
    bankIdIduraProduksjonAuthority: bankIdIduraProduksjonAuthority
    bankIdIduraProduksjonClientId: bankIdIduraProduksjonClientId
    bankIdIduraProduksjonClientSecret: bankIdIduraProduksjonClientSecret
    seedAdminPersonnummer: seedAdminPersonnummer
    seedAdminNavn: seedAdminNavn
    seedAdminMobilNr: seedAdminMobilNr
    seedAdminEpost: seedAdminEpost
    vippsClientId: vippsClientId
    vippsClientSecret: vippsClientSecret
    vippsSubscriptionKey: vippsSubscriptionKey
    vippsMerchantSerialNumber: vippsMerchantSerialNumber
    vippsMiljo: vippsMiljo
    vippsWebhookSecret: vippsWebhookSecret
    stripeSecretKey: stripeSecretKey
    stripePublishableKey: stripePublishableKey
    stripeWebhookSecret: stripeWebhookSecret
    stagingGateBasicAuthUsername: stagingGateBasicAuthUsername
    stagingGateBasicAuthPassword: stagingGateBasicAuthPassword
    varslingBaseUrl: varslingBaseUrl
    tillatUtviklingsSnarveier: tillatUtviklingsSnarveier
    tillatPersonnummerOverride: tillatPersonnummerOverride
    erBeta: erBeta
    ektBankIdProfesjonell: ektBankIdProfesjonell
    vippsBetaTestClientId: vippsBetaTestClientId
    vippsBetaTestClientSecret: vippsBetaTestClientSecret
    vippsBetaTestSubscriptionKey: vippsBetaTestSubscriptionKey
    vippsBetaTestMerchantSerialNumber: vippsBetaTestMerchantSerialNumber
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_WEB_ENDPOINT_URL string = resources.outputs.appServiceUrl
output AZURE_KEY_VAULT_NAME string = resources.outputs.keyVaultName
output MYSQL_SERVER_NAME string = resources.outputs.mysqlServerName
