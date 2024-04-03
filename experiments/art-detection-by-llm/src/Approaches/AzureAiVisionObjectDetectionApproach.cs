using System.Text;
using System.Text.Json;
using ArtDetection.Models;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace ArtDetection.Approaches;

public class AzureAiVisionObjectDetectionApproach(IConfiguration config) : IDetectArt
{
    private readonly IConfiguration _config = config;
    private readonly HttpClient _httpClient = new();

    public string ApproachShortName => "AiVision-ObjectDetection";

    public Polygon[] GetBoundingBoxes(string imageUrl)
    {
        List<Polygon> polygonBoundingBoxes = [];

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_config["AzureComputerVisionUrl"]}vision/v3.2/analyze?visualFeatures=Objects"),
            Headers = 
            {
                { "Ocp-Apim-Subscription-Key", _config["AzureComputerVisionKey"] }
            },
            Content = new StringContent("{\"url\":\"" + imageUrl + "\"}", Encoding.UTF8, "application/json")
        };
        var response = _httpClient.Send(request);
        var responseContent = response.Content.ReadAsStringAsync().Result;
        var responseObj = JsonSerializer.Deserialize<AnalyzeImageResponse>(responseContent);

        foreach (var obj in responseObj.objects)
        {
            Console.WriteLine($"Object detected: {obj.@object} ({obj.confidence}) [ {obj.rectangle.x}, {obj.rectangle.y}, {obj.rectangle.w}, {obj.rectangle.h} ]");
            PointF[] points = [
                new PointF(obj.rectangle.x, obj.rectangle.y),
                new PointF(obj.rectangle.x + obj.rectangle.w, obj.rectangle.y),
                new PointF(obj.rectangle.x + obj.rectangle.w, obj.rectangle.y + obj.rectangle.h),
                new PointF(obj.rectangle.x, obj.rectangle.y + obj.rectangle.h),
            ];
            var polygon = new Polygon(points);
            polygonBoundingBoxes.Add(polygon);
        }

        return [.. polygonBoundingBoxes];
    }
}
