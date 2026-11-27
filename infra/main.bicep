targetScope = 'resourceGroup'
@allowed(['staging','production'])
param environment string = 'staging'
param location string = resourceGroup().location
@secure()
param postgresPassword string
param authAuthority string
param authAudience string
param deployJobs bool = false
param deployServices bool = false
param images object = {}
param openAiModel string = ''
@secure()
param openAiApiKey string = ''
var suffix = uniqueString(resourceGroup().id, environment)
var prefix = 'mm-${environment}'
resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: '${prefix}-vnet'
  location: location
  properties: {
    addressSpace: { addressPrefixes: ['10.42.0.0/16'] }
    subnets: [
      { name: 'apps', properties: { addressPrefix: '10.42.0.0/23', delegations: [{ name: 'apps', properties: { serviceName: 'Microsoft.App/environments' } }] } }
      { name: 'postgres', properties: { addressPrefix: '10.42.2.0/24', delegations: [{ name: 'postgres', properties: { serviceName: 'Microsoft.DBforPostgreSQL/flexibleServers' } }] } }
      { name: 'endpoints', properties: { addressPrefix: '10.42.3.0/24', privateEndpointNetworkPolicies: 'Disabled' } }
    ]
  }
}
resource log 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}-logs'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}
resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${prefix}-insights'
  location: location
  kind: 'web'
  properties: { Application_Type: 'web', WorkspaceResourceId: log.id }
}
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = { name: '${prefix}-identity', location: location }
resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'mm${environment}${suffix}'
  location: location
  sku: { name: 'Premium' }
  properties: { adminUserEnabled: false, publicNetworkAccess: 'Disabled', networkRuleBypassOptions: 'AzureServices' }
}
resource registryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, 'pull')
  scope: registry
  properties: { principalId: identity.properties.principalId, principalType: 'ServicePrincipal', roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','7f951dda-4ed3-4680-a7ca-43fe172d538d') }
}
resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'mm-${take(environment,4)}-${suffix}'
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
    publicNetworkAccess: 'Disabled'
    networkAcls: { defaultAction: 'Deny', bypass: 'AzureServices' }
  }
}
resource secretsReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id,identity.id,'secrets')
  scope: vault
  properties: { principalId: identity.properties.principalId, principalType: 'ServicePrincipal', roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','4633458b-17de-408a-b874-0445c86b69e6') }
}
resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: 'mm${take(environment,3)}${suffix}'
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: { supportsHttpsTrafficOnly: true, minimumTlsVersion: 'TLS1_2', allowBlobPublicAccess: false, publicNetworkAccess: 'Disabled', allowSharedKeyAccess: false }
}
resource blob 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = { parent: storage, name: 'default', properties: { deleteRetentionPolicy: { enabled: true, days: 7 } } }
resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = { parent: blob, name: 'backups', properties: { publicAccess: 'None' } }
resource blobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = { name: guid(storage.id,identity.id,'blob'), scope: storage, properties: { principalId: identity.properties.principalId, principalType: 'ServicePrincipal', roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions','ba92f5b4-2d11-453d-a403-e96b0029c9fe') } }
var endpointDefinitions = [
  { name: 'vault', resource: vault.id, group: 'vault', zone: 'privatelink.vaultcore.azure.net' }
  { name: 'registry', resource: registry.id, group: 'registry', zone: 'privatelink.azurecr.io' }
  { name: 'blob', resource: storage.id, group: 'blob', zone: 'privatelink.blob.${az.environment().suffixes.storage}' }
]
resource dns 'Microsoft.Network/privateDnsZones@2024-06-01' = [for item in endpointDefinitions: { name: item.zone, location: 'global' }]
resource links 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = [for (item,i) in endpointDefinitions: { parent: dns[i], name: '${prefix}-link', location: 'global', properties: { virtualNetwork: { id: vnet.id }, registrationEnabled: false } }]
resource endpoints 'Microsoft.Network/privateEndpoints@2024-05-01' = [for item in endpointDefinitions: {
  name: '${prefix}-${item.name}-pe'
  location: location
  properties: { subnet: { id: vnet.properties.subnets[2].id }, privateLinkServiceConnections: [{ name: item.name, properties: { privateLinkServiceId: item.resource, groupIds: [item.group] } }] }
}]
resource zoneGroups 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2024-05-01' = [for (item,i) in endpointDefinitions: { parent: endpoints[i], name: 'default', properties: { privateDnsZoneConfigs: [{ name: item.name, properties: { privateDnsZoneId: dns[i].id } }] } }]
resource pgDns 'Microsoft.Network/privateDnsZones@2024-06-01' = { name: '${prefix}.private.postgres.database.azure.com', location: 'global' }
resource pgLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = { parent: pgDns, name: 'postgres-link', location: 'global', properties: { virtualNetwork: { id: vnet.id }, registrationEnabled: false } }
resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: '${prefix}-${suffix}-pg'
  location: location
  sku: { name: 'Standard_D2ds_v5', tier: 'GeneralPurpose' }
  properties: {
    version: '17'
    administratorLogin: 'metermind'
    administratorLoginPassword: postgresPassword
    storage: { storageSizeGB: 128 }
    backup: { backupRetentionDays: 14, geoRedundantBackup: 'Disabled' }
    network: { delegatedSubnetResourceId: vnet.properties.subnets[1].id, privateDnsZoneArmResourceId: pgDns.id, publicNetworkAccess: 'Disabled' }
    highAvailability: { mode: environment == 'production' ? 'ZoneRedundant' : 'Disabled' }
  }
  dependsOn: [pgLink]
}
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = { parent: postgres, name: 'metermind', properties: { charset: 'UTF8', collation: 'en_US.utf8' } }
resource dbSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = { parent: vault, name: 'database-connection', properties: { value: 'Host=${postgres.properties.fullyQualifiedDomainName};Database=metermind;Username=metermind;Password="${replace(postgresPassword,'"','""')}";SSL Mode=VerifyFull' } }
resource aiSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = { parent: vault, name: 'openai-api-key', properties: { value: empty(openAiApiKey) ? 'not-configured' : openAiApiKey } }
resource environmentResource 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${prefix}-apps'
  location: location
  properties: {
    vnetConfiguration: { infrastructureSubnetId: vnet.properties.subnets[0].id, internal: false }
    workloadProfiles: [{ name: 'Consumption', workloadProfileType: 'Consumption' }]
    appLogsConfiguration: { destination: 'log-analytics', logAnalyticsConfiguration: { customerId: log.properties.customerId, sharedKey: log.listKeys().primarySharedKey } }
  }
}
var commonEnvironment = [
  { name: 'ConnectionStrings__MeterDb', secretRef: 'database-connection' }
  { name: 'Auth__Authority', value: authAuthority }
  { name: 'Auth__Audience', value: authAudience }
  { name: 'Simulation__Enabled', value: 'true' }
  { name: 'DemoAuth', value: 'false' }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
]
resource jobs 'Microsoft.App/jobs@2024-03-01' = [for item in ['migration','simulator']: if(deployJobs) {
  name: '${prefix}-${item}'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    environmentId: environmentResource.id
    configuration: { triggerType: 'Manual', replicaTimeout: 3600, replicaRetryLimit: 0, manualTriggerConfig: { parallelism: 1, replicaCompletionCount: 1 }, registries: [{ server: registry.properties.loginServer, identity: identity.id }], secrets: [{ name: 'database-connection', keyVaultUrl: dbSecret.properties.secretUri, identity: identity.id }] }
    template: { containers: [{ name: item, image: images[item], env: commonEnvironment, resources: { cpu: 1, memory: '2Gi' } }] }
  }
  dependsOn: [registryPull,secretsReader,zoneGroups,database]
}]
resource api 'Microsoft.App/containerApps@2024-03-01' = if(deployServices) {
  name: '${prefix}-api'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentResource.id
    configuration: { activeRevisionsMode: 'Single', ingress: { external: false, targetPort: 8080, allowInsecure: false, transport: 'auto' }, registries: [{ server: registry.properties.loginServer, identity: identity.id }], secrets: [{ name: 'database-connection', keyVaultUrl: dbSecret.properties.secretUri, identity: identity.id },{ name: 'openai-key', keyVaultUrl: aiSecret.properties.secretUri, identity: identity.id }] }
    template: { containers: [{ name: 'api', image: images.api, env: concat(commonEnvironment,[{ name: 'OpenAI__Model', value: openAiModel },empty(openAiApiKey) ? { name: 'OpenAI__ApiKey', value: '' } : { name: 'OpenAI__ApiKey', secretRef: 'openai-key' }]), resources: { cpu: 1, memory: '2Gi' }, probes: [{ type: 'Liveness', httpGet: { path: '/health/live', port: 8080 }, initialDelaySeconds: 15 },{ type: 'Readiness', httpGet: { path: '/health/ready', port: 8080 }, initialDelaySeconds: 10 }] }], scale: { minReplicas: 1, maxReplicas: 1 } }
  }
  dependsOn: [registryPull,secretsReader,zoneGroups]
}
resource services 'Microsoft.App/containerApps@2024-03-01' = [for item in ['worker','mcp']: if(deployServices) {
  name: '${prefix}-${item}'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentResource.id
    configuration: { activeRevisionsMode: 'Single', ingress: item == 'mcp' ? { external: false, targetPort: 8080, allowInsecure: false, transport: 'auto' } : null, registries: [{ server: registry.properties.loginServer, identity: identity.id }], secrets: [{ name: 'database-connection', keyVaultUrl: dbSecret.properties.secretUri, identity: identity.id }] }
    template: { containers: [{ name: item, image: images[item], env: commonEnvironment, resources: { cpu: json('0.5'), memory: '1Gi' } }], scale: { minReplicas: 1, maxReplicas: 1 } }
  }
  dependsOn: [registryPull,secretsReader,zoneGroups]
}]
resource frontend 'Microsoft.App/containerApps@2024-03-01' = if(deployServices) {
  name: '${prefix}-frontend'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: environmentResource.id
    configuration: { activeRevisionsMode: 'Single', ingress: { external: true, targetPort: 8080, allowInsecure: false, transport: 'auto' }, registries: [{ server: registry.properties.loginServer, identity: identity.id }] }
    template: { containers: [{ name: 'frontend', image: images.frontend, env: [{ name: 'API_ORIGIN', value: 'https://${api!.properties.configuration.ingress.fqdn}' }], resources: { cpu: json('0.5'), memory: '1Gi' } }], scale: { minReplicas: 1, maxReplicas: 2 } }
  }
  dependsOn: [registryPull]
}
output registryName string = registry.name
output registryServer string = registry.properties.loginServer
output postgresName string = postgres.name
output environmentName string = environmentResource.name
output vaultName string = vault.name
output storageName string = storage.name
output frontendUrl string = deployServices ? 'https://${frontend!.properties.configuration.ingress.fqdn}' : ''
