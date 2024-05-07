using ArtDetection.Models;
using ArtDetection.Models.Gpt4WithVisionOnly;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using System.Text;
using System.Text.Json;

namespace ArtDetection.Approaches;

public class Gpt4WithVisionOnlyApproach(IConfiguration config) : IDetectArt
{
    private readonly IConfiguration _config = config;
    private readonly HttpClient _httpClient = new();

    public string ApproachShortName => "GPT4V-NoEnhancement";

    public Polygon[] GetBoundingBoxes(string imageUrl)
    {
        Polygon[] polygonBoundingBoxes = [];

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
                    enabled = false
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
                new () {
                    role = "system",
                    content = """
You are a helpful assistant that locates artwork in an image. You use the INSTRUCTIONS below to help identify the individual
pieces of art, and the RESPONSE DETAILS to help format your response.

-----------------
Instructions:
-----------------
- Do not treat the full image as an item of art.
- Locate every individual item of art in the image.
- Ignore things in the image that are not items of art (e.g., people or characteristics of the room).
- Create a rectangular bounding box around every identified item of art in the image for your response.
- The box should be as small as possible while still including the entire individual piece of art identified -- even if parts appear obscured by other objects.
- The box should not be bigger than the original image, and should be contained within the original image.
- The bounding box should include four points -- the top left, top right, bottom right, and bottom left.
- Each point should include an X and Y value in pixels representing the points relative location from the top left (0, 0) point of the overall image.

-----------------
Response details:
-----------------
Return the response as a JSON object as an array. DO NOT RETURN MARKDOWN! Each element in the array represents one identified piece of art.

Here is an example of what the JSON should look like (with topleft_x, topleft_y, topright_x, topright_y, bottomleft_x, 
bottom_left_y, bottomright_x, and bottomright_y being replaced for each item based on the pixel value for that piece of art's 
bounding box relative to the whole image):

[ 
""bounding_box"": [
{""x"": topleft_x, ""y"": topleft_y},
{""x"": topright_x, ""y"": topright_y},
{""x"": bottomright_x, ""y"": bottomright_y}
{""x"": bottomleft_x, ""y"": bottomleft_y},
]  
}
]

DO NOT provide this response as Markdown! Return only the raw JSON array.
"""
                },
                new () {
                    role = "user",
                    content = new object[]
                    {
                        new {
                            type = "text",
                            text = "List the bounding boxes for the pieces of art you found in the provided image."
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
            RequestUri = new Uri($"{_config["AzureOpenAIUrl"]}openai/deployments/gpt-4-v/extensions/chat/completions?api-version=2023-12-01-preview&modelVersion=latest"),
            Headers =
            {
                { "api-key", _config["AzureOpenAIKey"] }
            },
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        var response = _httpClient.Send(request);
        var responseContent = response.Content.ReadAsStringAsync().Result;
        var responseObj = JsonSerializer.Deserialize<ChatCompletionWithVisionResponse>(responseContent);

        if (responseObj != null && responseObj.choices.Count >= 1 && responseObj.choices[0].message.content != null)
        {
            var content = responseObj.choices[0].message.content.ToString();
            var boundingBoxes = JsonSerializer.Deserialize<List<PromptDefinedResponse>>(content);

            // Convert the bounding boxes to SixLabors.ImageSharp.Drawing.Polygon objects
            try
            {
                polygonBoundingBoxes = boundingBoxes.Select(bb =>
                    new Polygon(
                        bb.bounding_box.Select(p => new PointF(p.x, p.y)).ToArray()
                    )).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR: Couldn't process bounding box response: {e.Message}");
            }
        }
        else
        {
            Console.WriteLine("No response from GPT-4");
        }

        return polygonBoundingBoxes;
    }
}