param ingestionAppPrincipalId string
param backendAppPrincipalId string
param frontendAppPrincipalId string

param cognitiveServicesAccountName string
param storageAccountName string

param targetVectorDatabase string
param vectorDbResourceName string
var cosmosAccountName = (targetVectorDatabase == 'CosmosDb') ? vectorDbResourceName : ''
var searchServiceName = (targetVectorDatabase == 'AiSearch') ? vectorDbResourceName : ''

// -----------------------------------------------
// Built-in Roles
//

// Azure RBAC
var cognitiveServicesUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
var storageBlobDataOwnerRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')

// If targetVectorDatabase == 'AiSearch'...
var searchServiceContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7ca78c08-252a-4471-8644-bb5ff32d4ba0')
var searchServiceIndexDataContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
var searchServiceIndexDataReaderRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')

// If targetVectorDatabase == 'CosmosDb'...
var documentDbAccountContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5bd9cd88-fe45-4216-938b-f97437e15450')

// Azure Cosmos DB RBAC
var cosmosDbDataReaderRole = (targetVectorDatabase == 'CosmosDb') ? resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', cosmosAccountName, '00000000-0000-0000-0000-000000000001') : ''
var customIngestionRoleDefinitionName = 'Custom Super User Role for Ingestion'
var customIngestionRoleDefinitionId = (targetVectorDatabase == 'CosmosDb') ? guid(subscription().id, resourceGroup().id, cosmosAccountName, customIngestionRoleDefinitionName) : ''

// -----------------------------------------------
// Target resources
//

resource cognitiveServicesAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: cognitiveServicesAccountName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = if (targetVectorDatabase == 'CosmosDb') {
  name: cosmosAccountName
}

resource searchService 'Microsoft.Search/searchServices@2024-06-01-preview' existing = if (targetVectorDatabase == 'AiSearch') {
  name: searchServiceName
}

// -----------------------------------------------
// Ingestion App
//

resource ingestionCognitiveServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, ingestionAppPrincipalId, cognitiveServicesUserRole)
  properties: {
    principalId: ingestionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: cognitiveServicesUserRole
  }
  scope: cognitiveServicesAccount
}

resource ingestionStorageBlobDataOwner 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, ingestionAppPrincipalId, storageBlobDataOwnerRole)
  properties: {
    principalId: ingestionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: storageBlobDataOwnerRole
  }
  scope: storageAccount
}

// If targetVectorDatabase == 'CosmosDb'...

resource ingestionDocumentDbAccountContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (targetVectorDatabase == 'CosmosDb') {
  name: guid(subscription().id, resourceGroup().id, ingestionAppPrincipalId, documentDbAccountContributorRole)
  properties: {
    principalId: ingestionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: documentDbAccountContributorRole
  }
  scope: cosmosAccount
}

resource ingestionCustomSqlRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = if (targetVectorDatabase == 'CosmosDb') {
  parent: cosmosAccount
  name: customIngestionRoleDefinitionId
  properties: {
    roleName: customIngestionRoleDefinitionName
    type: 'CustomRole'
    assignableScopes: [
      cosmosAccount.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/write'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
      }
    ]
  }
}

resource ingestionSqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = if (targetVectorDatabase == 'CosmosDb') {
  parent: cosmosAccount
  name: guid(subscription().id, resourceGroup().id, cosmosAccount.id, ingestionAppPrincipalId, ingestionCustomSqlRoleDefinition.id)
  properties: {
    principalId: ingestionAppPrincipalId
    roleDefinitionId: ingestionCustomSqlRoleDefinition.id
    scope: cosmosAccount.id
  }
}

// If targetVectorDatabase == 'AiSearch'...

resource ingestionSearchServiceContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (targetVectorDatabase == 'AiSearch') {
  name: guid(subscription().id, resourceGroup().id, ingestionAppPrincipalId, searchServiceContributorRole)
  properties: {
    principalId: ingestionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: searchServiceContributorRole
  }
  scope: searchService
}

resource ingestionSearchIndexDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (targetVectorDatabase == 'AiSearch') {
  name: guid(subscription().id, resourceGroup().id, ingestionAppPrincipalId, searchServiceIndexDataContributorRole)
  properties: {
    principalId: ingestionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: searchServiceIndexDataContributorRole
  }
  scope: searchService
}

// -----------------------------------------------
// UI Backend App
//

resource backendCognitiveServicesUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, backendAppPrincipalId, cognitiveServicesUserRole)
  properties: {
    principalId: backendAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: cognitiveServicesUserRole
  }
  scope: cognitiveServicesAccount
}

// If targetVectorDatabase == 'CosmosDb'...

resource backendSqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = if (targetVectorDatabase == 'CosmosDb') {
  parent: cosmosAccount
  name: guid(subscription().id, resourceGroup().id, cosmosAccount.id, backendAppPrincipalId, cosmosDbDataReaderRole)
  properties: {
    principalId: backendAppPrincipalId
    roleDefinitionId: cosmosDbDataReaderRole
    scope: cosmosAccount.id
  }
}

// If targetVectorDatabase == 'AiSearch'...

resource backendSearchIndexDataReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (targetVectorDatabase == 'AiSearch') {
  name: guid(subscription().id, resourceGroup().id, backendAppPrincipalId, searchServiceIndexDataReaderRole)
  properties: {
    principalId: backendAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: searchServiceIndexDataReaderRole
  }
  scope: searchService
}
