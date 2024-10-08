# Search for semantically similar images with image data

**Welcome!** Want to see this experiment as part of a full end-to-end reference implementation? This experiment led to the development of the backend component in the [semantic search for images reference implementation](../../reference-implementations/semantic-search-for-images/README.md). Look there for a complete solution that may be deployed to your Azure subscription.

## Goal

Vector search is extremely powerful for text embeddings. With Azure AI Vision's multimodal image embeddings that semantically similar search can be applied to image data as well.

## Approach

For this implementation, an Azure Function app (C# isolated) was implemented to support HTTP requests for the closest semantic match to either a provided image or text. The function is responsible for:
  - Generating a multimodal image embedding from the provided image (via URL or stream) or text via the AI Vision v4 Multimodal Embeddings APIs
  - Creating an Azure AI Search vector query with the embedding
  - Returning the top N (default: three) highest confidence matches from the targeted AI Search index

It assumes an Azure AI Search index has already been created that includes image embeddings generated from the same model used by the search function. If model versions are not aligned, the search will provide poor results! Just like with text embedding models, it's important to use the same model version and API to generate the query embedding and the index embeddings.

### Resources Summary

| Azure Resource | Config |
| -------------- | ------ |
| AI Search | Refer to the [vector search quickstart](https://learn.microsoft.com/en-us/azure/search/search-get-started-vector) for any notes or constraints. |
| Computer Vision | See any notes or regional constraints in the [docs](https://learn.microsoft.com/en-us/azure/ai-services/computer-vision/how-to/image-retrieval?tabs=csharp). |

### Findings

As long as the same multimodal embeddings model is used on all sides, the accuracy of the results is strong. This approach may well replace the need for custom vision models and classification models as it supports image-to-image similarity search as well as text-to-image similarity search.

## Alternate Approaches

The main variance here would be the mechanism to host the compute responsible for making the call to the AI Vision multimodal embeddings API and search query against AI Search. While this was written as an Azure Function, any programming language and compute would support the goal.