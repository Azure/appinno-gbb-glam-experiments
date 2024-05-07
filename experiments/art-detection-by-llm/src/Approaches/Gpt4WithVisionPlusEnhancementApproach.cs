using ArtDetection.Models;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using System.Text;
using System.Text.Json;

namespace ArtDetection.Approaches;

public class Gpt4WithVisionPlusEnhancementApproach(IConfiguration config) : IDetectArt
{
    private readonly IConfiguration _config = config;
    private readonly HttpClient _httpClient = new();

    public string ApproachShortName => "GPT4V-PlusEnhancement";

    public Polygon[] GetBoundingBoxes(string imageUrl)
    {
        List<Polygon> polygonBoundingBoxes = [];

        var requestBody = new ChatCompletionWithVisionRequest()
        {
            enhancements = new Enhancements
            {
                ocr = new Ocr
                {
                    enabled = false
                },
                grounding = new Grounding
                {
                    enabled = true
                }
            },
            dataSources =
            [
                new() {
                    type = "AzureComputerVision",
                    parameters = new Parameters
                    {
                        endpoint = _config["AzureComputerVisionUrl"],
                        key = _config["AzureComputerVisionKey"]
                    }
                }
            ],
            messages =
            [
                new() {
                    role = "system",
                    content = """
You are a helpful assistant that locates artwork in an image.
Your responses should only include the pieces of artwork with no additional information.
DO NOT include references to other things in the image, unless it is required to help identify the artwork.
Ignore any parts of the image that are not a painting or sculpture.

Example responses:
------------------
User: Find the art in the provided image.
Assistant: Three paintings along the wall
------------------
User: Find the art in the provided image.
Assistant: Two paintings on the wall and one sculpture in the middle of the room
------------------
User: Find the art in the provided image.
Assistant: Four paintings across two walls
------------------

Assistant:
"""
                },
                new() {
                    role = "user",
                    content = new object[] 
                    {
                        new {
                            type = "text",
                            text = "Find art in the provided image."
                        },
                        new {
                            type = "image_url",
                            image_url = new {
                                url = imageUrl
                            }
                        }
                    }
                }
            ],
            max_tokens = 1000,
            stream = false
        };

        // Make a request to GPT-4 with Vision

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_config["AzureOpenAIUrl"]}openai/deployments/{_config["AzureOpenAIDeploymentName"]}/extensions/chat/completions?api-version={_config["AzureOpenAIApiVersion"]}&modelVersion={_config["AzureOpenAIModelVersion"]}"),
            Headers =
            {
                { "api-key", _config["AzureOpenAIKey"] }
            },
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = _httpClient.Send(request);
        var responseContent = response.Content.ReadAsStringAsync().Result;
        var responseObj = JsonSerializer.Deserialize<ChatCompletionWithVisionResponse>(responseContent);

        // Download the image that was analyzed because the bounding box from the enhancement
        // is relative to the original, we need the original width and height to get the correct Polygon
        var imageResponse = _httpClient.GetAsync(imageUrl).Result;
        var imageStream = imageResponse.Content.ReadAsStream();
        var originalImage = Image.Load(imageStream);

        foreach (var choice in responseObj.choices)
        {
            foreach (var line in choice.enhancements.grounding.lines)
            {
                foreach (var span in line.spans)
                {

                    var points = span.polygon.Select(p => new PointF((float)(p.x * originalImage.Width), (float)(p.y * originalImage.Height))).ToArray();
                    var polygon = new Polygon(points);

                    Console.WriteLine($"Grounding enhancement span: {line.text}, {span.text} [ {polygon.Bounds.Top}, {polygon.Bounds.Left}, {polygon.Bounds.Width}, {polygon.Bounds.Height} ]");
                    polygonBoundingBoxes.Add(polygon);
                }
            }
        }

        return [.. polygonBoundingBoxes];
    }

}