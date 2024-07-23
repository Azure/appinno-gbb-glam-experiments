# GLAM Experiments

> "GLAM is an acronymn for galleries, libraries, archives, and museums, and refers to cultural institutions with a mission to provide access to knowledge."
> <div align="right">from <a href="https://en.wikipedia.org/wiki/GLAM_(cultural_heritage)">Wikipedia</a></div>

## Purpose

This repository contains several samples in support of cultural institution use cases with Generative AI and advanced AI Vision capabilities. Samples are either small experiments used to help understand the underlying tech and capabilities, or they're reference implementations of identified end-to-end solutions or patterns. If you're looking for an end-to-end solution that can be provisioned and deployed to your Azure subscription, start in the [`/reference-implementations`](#reference-implementations). If you're looking for targeted code that was created to better understand the problem space, head to the [`/experiments`](#experiments).

### Reference Implementations
---
- [Semantic Search for Images](./reference-implementations/semantic-search-for-images/README.md)

---

Reference implementations provide an end-to-end solution for a pattern or use case. Each reference impementation should include:
| Artifact | Description |
|----------|-------------|
| `azure.yaml` file | An [Azure Developer CLI (`azd`)](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/overview) specification that describes the application for one-step infrastructure provisioning and application deployment. |
| `README.md` file | Describes the pattern or use case and information on how to build, run, and deploy the implementation |
| `infra/` directory | Contains deployment scripts, Bicep, or other deployment artifacts so that the solution may be deployed |
| `src/` directory | Contains the source code implementation -- either a single implementation or multiple implementations using different languages, frameworks, or dependencies all implementing the same pattern |

### Experiments
---
- [Creating an Azure AI Search index including multimodal image embeddings (vectors)](./experiments/create-search-index/README.md)
- [Searching for most semantically similar images given an image or text](./experiments/semantic-search-with-images/README.md)
- [Leveraging an LLM with vision to identify works of art in a broader image](./experiments/art-detection-by-llm/README.md)

---

Many of the experiments required some foundational data against which to search or use for LLM grounding. This repository includes a sample that builds an Azure AI Search index with works available from the [National Gallery of Art's Open Data Program](https://github.com/NationalGalleryOfArt/opendata) or [The Met's Open Access Initiative](https://www.metmuseum.org/about-the-met/policies-and-documents/open-access), but any foundational data may be used.

Experiments are not standardized -- they may use different programming languages, execution models, or just include written walkthroughs guiding through the online portal-based tooling. The intention is to capture what was investigated, and outcomes of those experiments may be used in a more cohesive end-to-end solution related to specific use cases and patterns.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
