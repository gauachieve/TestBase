@description('Azure-region for alle ressurser')
param location string

@description('Kort, unik streng brukt til å avlede globalt unike ressursnavn')
param resourceToken string

@description('Tags som skal settes på alle ressurser')
param tags object

@description('Administrator-brukernavn for MySQL Flexible Server')
param mysqlAdministratorLogin string

@description('Delt nøkkel for StagingGate — tom verdi deaktiverer sperren, se main.bicep')
@secure()
param stagingGateAccessKey string = ''

@description('Forhåndsregistrert alfanumerisk SMS-avsender-ID — tom verdi gir MockSmsSender, se main.bicep')
param smsSenderId string = ''

// Testmiljø uten ekte pasientdata — passordet genereres deterministisk og lagres kun i Key Vault.
var mysqlAdministratorPassword = 'Tb${uniqueString(resourceGroup().id, resourceToken)}!26'

var appServicePlanName = 'plan-testbase-${resourceToken}'
var appServiceName = 'app-testbase-${resourceToken}'
var mysqlServerName = 'mysql-testbase-${resourceToken}'
var keyVaultName = 'kv-tb-${take(resourceToken, 17)}'
var databaseName = 'testbase_test'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource mysqlServer 'Microsoft.DBforMySQL/flexibleServers@2023-06-30' = {
  name: mysqlServerName
  location: location
  tags: tags
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: mysqlAdministratorLogin
    administratorLoginPassword: mysqlAdministratorPassword
    version: '8.0.21'
    storage: {
      storageSizeGB: 20
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

// Testmiljø: App Service har ingen fast utgående IP uten VNet-integrasjon, så vi tillater
// Azure-interne IP-er. Ingen ekte pasientdata lagres her — se CLAUDE.md.
resource mysqlFirewallAzure 'Microsoft.DBforMySQL/flexibleServers/firewallRules@2023-06-30' = {
  parent: mysqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource mysqlDatabase 'Microsoft.DBforMySQL/flexibleServers/databases@2023-06-30' = {
  parent: mysqlServer
  name: databaseName
  properties: {
    charset: 'utf8mb4'
    // MySQL 8.0 normaliserer 'utf8mb4_general_ci' til sin egen default 'utf8mb4_0900_ai_ci'
    // ved opprettelse — deklarer den faktiske verdien, ellers feiler HVER senere
    // "azd provision" med "DatabaseCharsetOrCollationConflict" (ARM/REST støtter ikke å
    // endre collation i ettertid, kun via en ekte SQL ALTER DATABASE).
    collation: 'utf8mb4_0900_ai_ci'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    softDeleteRetentionInDays: 7
  }
}

var mysqlConnectionString = 'Server=${mysqlServer.properties.fullyQualifiedDomainName};Port=3306;Database=${databaseName};User=${mysqlAdministratorLogin};Password=${mysqlAdministratorPassword};SslMode=Required;'

resource connectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'DefaultConnection'
  properties: {
    value: mysqlConnectionString
  }
}

// E-postutsending (invitasjoner, rapport-varsler, påminnelser) via Azure Communication
// Services — Azure-native, ingen egen leverandøravtale nødvendig (samme abonnement som
// resten av infrastrukturen). Azure-administrert domene: ingen DNS-verifisering nødvendig,
// men Azure velger selv det faktiske avsenderdomenet (noe-generert.azurecomm.net) — leses ut
// etter provisjonering, se docs/beslutningslogg.md.
var communicationServiceName = 'acs-testbase-${resourceToken}'
var emailServiceName = 'email-testbase-${resourceToken}'

resource emailService 'Microsoft.Communication/emailServices@2023-04-01' = {
  name: emailServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'Norway'
  }
}

resource emailDomain 'Microsoft.Communication/emailServices/domains@2023-04-01' = {
  parent: emailService
  name: 'AzureManagedDomain'
  location: 'global'
  tags: tags
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource emailSenderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-04-01' = {
  parent: emailDomain
  name: 'noreply'
  properties: {
    username: 'noreply'
    displayName: 'TestBase (testmiljø)'
  }
}

resource communicationService 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: communicationServiceName
  location: 'global'
  tags: tags
  properties: {
    dataLocation: 'Norway'
    linkedDomains: [
      emailDomain.id
    ]
  }
}

resource acsConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'AcsConnectionString'
  properties: {
    value: communicationService.listKeys().primaryConnectionString
  }
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Development'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${connectionStringSecret.properties.secretUri})'
        }
        {
          name: 'Acs__ConnectionString'
          value: '@Microsoft.KeyVault(SecretUri=${acsConnectionStringSecret.properties.secretUri})'
        }
        {
          name: 'Email__SenderAddress'
          value: 'noreply@${emailDomain.properties.mailFromSenderDomain}'
        }
        {
          // Tom verdi => StagingGate.cs deaktiverer sperren (se Program.cs). Denne
          // MÅ stå i denne listen — appSettings på Microsoft.Web/sites er en FULL
          // erstatning ved hver "azd provision", ikke en sammenslåing, så en nøkkel
          // satt kun via "az webapp config appsettings set" forsvinner ved neste
          // provision (skjedde 2026-09-02, se docs/beslutningslogg.md).
          name: 'StagingGate__AccessKey'
          value: stagingGateAccessKey
        }
        {
          name: 'Sms__SenderId'
          value: smsSenderId
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

resource kvSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, appService.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: appService.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
  }
}

output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output keyVaultName string = keyVault.name
output mysqlServerName string = mysqlServer.name
