# Reference Implementation: Semantic Search with Images

## Overview

### Solution

```mermaid
flowchart LR
    subgraph Components
        direction LR
        subgraph UI
            Frontend <--> Backend
        end
        Backend <--> vectorAPI([Multimodal Embeddings API]) & vectorDB[(Vector DB)] <--> ingestProc[Data Ingestion/Load]
        subgraph Ingest
            ingestProc ~~~ source[["Source CSV File(s)"]]
            source .-> ingestProc
        end
    end
    user((User)) --> Frontend
```

| Component | Description |
|-----------|-------------|
| Frontend | A web application providing an interface for entering search text and/or providing an image and displaying search results. |
| Backend | A RESTful API supporting searching for sematically similar images by text or image. |
| Multimodal Embeddings API | The *Vectorize Image API* and *Vectorize Text API* provided by Azure AI Vision. |
| Vector DB | A vector database that supports storage of embeddings and retrieval. |
| Data Ingestion / Load | A process triggered by CSV file(s) responsible for vectorizing the image and saving data to the data store. |
| Source CSV File(s) | Required: One or more CSV files of the specified columnar format to load or seed the data store. |

### Resources

| Azure Resource | Notes |
|----------------|-------|
| Azure AI Services (Multi-Service) *or* Computer Vision | [[Concept Docs]](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/concept-image-retrieval) [[API Docs]](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/how-to/image-retrieval) [[API Spec]](https://learn.microsoft.com/en-us/rest/api/computervision/vectorize?view=rest-computervision-2024-02-01) <br/>* Limited regional availability. Available in U.S. regions EastUS, WestUS, and WestUS2 (but always check documentation for up-to-date changes). |
| (Vector DB Option) Azure Cosmos DB - NoSQL API | [[Concept Docs]](https://learn.microsoft.com/en-us/azure/cosmos-db/vector-database#nosql-api) [[Vector Search (Preview) Enrollment, Policies, and Search]](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/vector-search) <br/>* DiskANN index requires early gated-preview enrollment with [this form](https://aka.ms/DiskANNSignUp). <br/>* Shared throughput databases can't use vectors search preview at this time. <br/>* Supports vector search, but if more complex search requirements need support consider using as [source to an Azure AI Search index](https://learn.microsoft.com/en-us/azure/search/search-howto-index-cosmosdb). |
| (Vector DB Option) Azure AI Search | [[Concept Docs]](https://learn.microsoft.com/en-us/azure/search/vector-search-overview) [[Creating vector index]](https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-create-index?tabs=config-2023-11-01%2Crest-2023-11-01%2Cpush%2Cportal-check-index) [[Querying vectors]](https://learn.microsoft.com/en-us/azure/search/vector-search-how-to-query?tabs=query-2023-11-01%2Cfilter-2023-11-01) <br/>* Supports advanced search capabilities including: <br/>&emsp;- Hybrid search mixing keywords and vectors <br/>&emsp;- Filtering nonvector fields to reduce possible matches <br/>&emsp;- Multiple vector fields <br/>&emsp;- Multple vector queries (e.g., against embedded text *and* embedded images) <br/>&emsp;- (Preview) vector weighting to tune search scores <br/>&emsp;- [Semantic ranking](https://learn.microsoft.com/en-us/azure/search/semantic-search-overview) |
| Azure Container App Environment | [[Overview]](https://learn.microsoft.com/en-us/azure/container-apps/overview) |
| Azure Blob Storage | [[Overview]](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction) |

## How to Deploy and Run in Azure

### Prerequisites

- Azure Subscription
- RBAC Permissions to create resources in target resource group
- CSV formatted file containing data to pre-load (see [definition](#data-ingestion-format-csv))
- [Azure Developer CLI](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/install-azd) for managing key workflow stages (coding, building, deploying, and monitoring) in a platform-agnostic way
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) for some custom deployment script capabilities
- [Docker or Docker Alternative]
- Application-specific requirements:
    - [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) to develop and build the Ingestion and Backend services
    - [Node.js](https://nodejs.org/en) to develop and build the Frontend service

### Use Azure Developer CLI

1. Login to Azure Developer CLI with `azd auth login`.

1. Run `azd up`.
    - Enter an environment name (e.g., *semantic-search-for-images*)
    - Select the appropriate target Azure Subscription
    - Select the appropriate target location
    - Select the target vector database (either Azure AI Search [AiSearch], or Azure Cosmos DB NoSQL API [CosmosDb])
    > Note: You can re-run `azd up` as many times as you like to both provision and deploy updates to the application. If provisioning parameters change, just re-run. If application code changes, just re-run.  

    > IMPORTANT: There is a known issue where at times the first run fails with an error related to not finding the ui-backend and/or ingestion services. If you hit this error, re-run `azd up`, and it should successfully provision all resources when running the second time.

1. Wait for the Azure Developer CLI to provision resources, package the applications, and deploy the applications to Azure.

1. Drop your CSV file into the provisioned storage account for ingestion.
    - Upload via Portal, CLI, or Storage Explorer
    > Note: the ingestion may take some time, so you can check the Azure Container Apps Job Execution History to see if it has completed.

1. Navigate to the UI Frontend

1. (Optional) When finished with the deployment, deprovision all resources with `azd down`.

### Data Ingestion Format (CSV)

| Column (*\* required*) | Description |
|------------------------|-------------|
| objectId* | A unique identifier for the image |
| imageUrl* | A publicly-accessible URL path to the image |
| title | The title for the object |
| artist | The primary artist for the object |
| creationDate | A display date (text) representing when the object was created (e.g., "c. 1650", "1911-12", "1834") |

The first line of the file should be the header line, and the column names will be matched against those provided above. If the required columns cannot be matched, ingestion will fail. If an optional column does not match one of the predefined columns, it will be added under a metadata field for the item.

Example:
```csv
objectId,imageUrl,title,artist,creationDate,medium,dimensions
"123","https://image-host-location/objects/123.jpg","asdf","Smith, John","1974","Oil","24in. x 24in."
```

## How to Run Locally

### UI - Frontend

0. Prerequisites
   - [Node.js](https://nodejs.org/en)
   - Consider your working directory `src/ui-frontend/`
1. Create local environment setup
   - Copy `.env-sample` to `.env.development.local`
   - Set the API URLs. If using a locally-running version of the backend service:
      - Update `REACT_APP_AZURE_TEXT_API_URL` with `http://localhost:5183/api/SemanticSearch/text`
      - Update `REACT_APP_AZURE_IMAGE_API_URL` with `http://localhost:5183/api/SemanticSearch/imageStream`
2. Run `npm install`
3. Run `npm start`
   - Application should be available at `http://localhost:3000/`

### UI - Backend

0. Prerequisites
   - [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
   - Consider your working directory `src/ui-backend/`
1. Create local `appsettings.json` file with local connectivity settings
   - Copy `example.appsettings.json` to `appsettings.json`
   - Pick the Vector DB implementation you're targeting (`CosmosDb` or `AiSearch` for property `DatabaseTargeted`)
      - Update the appropriate target Uri with a deployed resource to which you have access
   - Update the `AiServices.Uri` with a deployed resource to which you have access
2. Run `dotnet build`
3. Run `dotnet run` to run the application
   - Service should be available at `http://localhost:5183/`

### Ingestion

0. Prerequisites
   - [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
   - Consider your working directory `src/ingestion/`
1. Create local `appsettings.json` file with local connectivity settings
   - Copy `example.appsettings.json` to `appsettings.json`
   - Pick the Vector DB implementation you're targeting (`CosmosDb` or `AiSearch` for property `DatabaseTargeted`)
      - Update the appropriate target Uri with a deployed resource to which you have access
   - Update the `AiServices.Uri` with a deployed resource to which you have access
   - Update the `StorageAccount.Uri` with a deployed resource to which you have access
2. Run `dotnet build`
3. Run `dotnet run` to run the application
   - Application will look to the *appsettings.json*-specified storage account to see begin the ingestion process
