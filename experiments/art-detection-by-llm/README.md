# Identify and locate works of art in a broader image

## Goal

Assuming an image containing a broad field of view across a gallery, identify the works of art and provide their bounding boxes relative to the source image.

## Approach

This experimental implementation is a C# Console Application that is responsible for:
  - Asking for bounding boxes from different approaches (LLM, LLM + AI Vision enhancement, and AI Vision object detection)
  - Drawing the bounding boxes on the source image(s)
  - Saving the image(s) locally for review

### Best Results: Azure OpenAI - GPT-4 Turbo with Vision Model - Plus AI Vision enhancement

The request to the LLM is managed as an chat completion API call to Azure OpenAI. It requires a model deployment that supports vision (e.g., model `gpt-4`, version `vision-preview`). Check the [docs](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#standard-deployment-model-availability) for current information on deployment model availability. It also uses the AI Vision enhancement to generate the bounding boxes. This requires a Computer Vision deployment.

### Resources Summary

| Azure Resource | Config |
| -------------- | ------ |
| Computer Vision | See any notes or regional constraints in the [docs](https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/gpt-with-vision?tabs=rest%2Csystem-assigned%2Cresource#use-vision-enhancement-with-images). |
| OpenAI Service | Deployment model: `gpt-4`, version `vision-preview`. Limited regional availability, so check the [docs](https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models#standard-deployment-model-availability) for current information. |

### Findings

The LLM is great at finding the objects, but sometimes that doesn't align exactly with what's being called out from the enhancement. This can lead to difficulty finding only bounding boxes for the identified art. For instance, in some cases it would correctly identify the art, but the bounding box would be a single large box around all pieces. In other cases, it would correctly identify the art but also include bounding boxes around other objects or persons in the field of view.

The prompt used in this sample provided generally good results given different types of source images. It doesn't always get it exactly right, but it more often did than other attempts.

## Alternate Approaches

### Azure OpenAI - GPT-4 Turbo with Vision Model - No AI Vision enhancement

Without the AI Vision enhancement, it was difficult to generate correct bounding boxes for the identified art. The LLM verbally described them correctly and seemed to identify them correctly, but it wasn't able to provide accurate bounding boxes.

### Azure AI Vision - Object Detection API

The object detection API provided as part of AI Vision did not identify artwork as objects. It would require a custom vision model trained to identify artwork.

### Azure AI Vision - Custom Vision model

This approach was not tested due to the time and effort needed to train a custom vision model for art detection. We believe this approach would support the use case well, but it does require more work and maintenance to ensure the object detection is working as expected. This approach would not require an LLM in the mix, and also it would be focused on the task instead of a general model being refined via prompting.