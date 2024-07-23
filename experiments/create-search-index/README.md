# Create an Azure AI Search Index including Multi-Modal Image Embeddings (Vectors)

**Welcome!** Want to see this experiment as part of a full end-to-end reference implementation? This experiment led to the development of the ingestion component in the [semantic search for images reference implementation](../../reference-implementations/semantic-search-for-images/README.md). Look there for a complete solution that may be deployed to your Azure subscription.

## Goal

Leverage Azure AI Vision's multimodal embeddings APIs to support semantic search across image-based documents (e.g., art objects) based on the image data itself.

## Approach: Locally Run Console Application with Document Upload

For this implementation, we used a C# Console Application that was executed locally to populate the index. It was responsible for:
  - Querying the NGA Open Data Program data or The Met's Open Access APIs to get all objects
  - For all objects with an image URL:
    - Generate multi-modal image embeddings via the AI Vision v4 API
    - Create and upload a document to the AI Search index including the embeddings and metadata from the object

If the image URL does not exist, the object is skipped. If any errors are encountered generating the embeddings from the image, the object is skipped.

The [National Gallery of Art Open Data Program](https://github.com/NationalGalleryOfArt/opendata) provides CSV-formatted files that can be loaded into a relational database. A PostgreSQL database is recommended, and the console app assumes that database exists and has been seeded if you choose to use NGA as a source. 

[The Metropolitan Museum of Art Open Access Initiative](https://www.metmuseum.org/about-the-met/policies-and-documents/open-access) makes all images of public-domain artworks and basic data on all accessioned works in its collection available for unrestricted use under Creative Commons Zero. While a CSV file is available, this experiment includes a pre-processor that uses their [API](https://metmuseum.github.io/) to pull a list of all objects with images and then stores all available object data in an Azure Cosmos DB container.

### Resources Summary

| Azure Resource | Config |
| -------------- | ------ |
| AI Search | Refer to the [vector search quickstart](https://learn.microsoft.com/en-us/azure/search/search-get-started-vector) for any notes or constraints. |
| Computer Vision | See any notes or regional constraints in the [docs](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/how-to/image-retrieval?tabs=csharp). |
| Database for PostgreSQL - Flexible Server | **Required: NGA** Assumes it exists and has been seeded with NGA's Open Data Program data. Check the docs to see how to create an instance using the [portal](https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/quickstart-create-server-portal) or [Azure CLI](https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/quickstart-create-server-cli). |
| Azure Cosmos DB - NoSQL | **Required: MET** Check the docs to see how to create an instance using the [portal](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/quickstart-portal). |

### Findings

The brute-force approach of creating individual search index documents and batch uploading them works for this purpose. We assume a static set of data and a process that will run once. If the index needs to be updated over time, consider using an Indexer.

## Alternate Approaches

### Azure AI Search - Indexer with Custom Skillset & Custom Web Skill to retrieve image embeddings

This approach provides several advantages. First, it utilizes Azure AI Search compute to run the indexing process so there are no additional hosting requirements. Second, the record of all runs including status, errors, success, and number of search index documents created/modified can be managed and monitored from within the Azure AI Search resource.

We did not implement this approach because it does require a Custom Web Skill implementation to be able to call out to the AI Vision API to get the embeddings for the URL. Custom Web Skills must implement a specific contract in order to be plugged into a Custom Skillset, so we would need to host an API to handle those details.

### Azure AI Search - Import Vector Data

This path assumes text-based embeddings and is not capable of generating image-based embeddings.