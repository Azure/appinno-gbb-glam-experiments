param ingestionAppPrincipalId string
param backendAppPrincipalId string
param frontendAppPrincipalId string

param cognitiveServicesAccountName string
param storageAccountName string
param cosmosAccountName string

// -----------------------------------------------
// Built-in Roles
//

// Azure RBAC
var cognitiveServicesUserRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
var storageBlobDataOwnerRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b')
var documentDbAccountContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5bd9cd88-fe45-4216-938b-f97437e15450')
// Azure Cosmos DB RBAC
var cosmosDbDataReaderRole = resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', cosmosAccountName, '00000000-0000-0000-0000-000000000001')
var customIngestionRoleDefinitionName = 'Custom Super User Role for Ingestion'
var customIngestionRoleDefinitionId = guid(subscription().id, resourceGroup().id, cosmosAccountName, customIngestionRoleDefinitionName)

// -----------------------------------------------
// Target resources
//

resource cognitiveServicesAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: cognitiveServicesAccountName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmosAccountName
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

resource ingestionDocumentDbAccountContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, ingestionAppPrincipalId, documentDbAccountContributorRole)
  properties: {
    principalId: ingestionAppPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: documentDbAccountContributorRole
  }
  scope: cosmosAccount
}

resource ingestionCustomSqlRoleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-05-15' = {
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

resource ingestionSqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosAccount
  name: guid(subscription().id, resourceGroup().id, cosmosAccount.id, ingestionAppPrincipalId, ingestionCustomSqlRoleDefinition.id)
  properties: {
    principalId: ingestionAppPrincipalId
    roleDefinitionId: ingestionCustomSqlRoleDefinition.id
    scope: cosmosAccount.id
  }
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

resource backendSqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: cosmosAccount
  name: guid(subscription().id, resourceGroup().id, cosmosAccount.id, backendAppPrincipalId, cosmosDbDataReaderRole)
  properties: {
    principalId: backendAppPrincipalId
    roleDefinitionId: cosmosDbDataReaderRole
    scope: cosmosAccount.id
  }
}

// -----------------------------------------------
// UI Frontend App
//
// TODO: MAY NOT USE... API is open, but we will lock down to internal
// ingress only on ACA side... so frontend will be able to access, but
// anything else outside of ACA environment would not.
