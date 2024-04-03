using ArtDetection.Approaches;
using Microsoft.Extensions.Configuration;

namespace ArtDetection;

class Program
{
    static void Main(string[] args)
    {
        /*
           Be sure to create an appsettings.json file according to the sample.appsettings.json file provided in the root of this project.

           Uncomment one of the approaches below to execute it. The IDetectArt implementations include:
            - Gpt4WithVisionOnlyApproach: Uses GPT-4 Turbo w/Vision only. The prompt includes a request
                to format the output to include bounding boxes of a specific format for any art identified.
            - Gpt4WithVisionPlusEnhancementApproach: Uses GPT-4 Turbo w/Vision plus the AI Vision enhancement
                to support additional grounding. The prompt is tuned towards encouraging the response to
                include only Azure AI Vision object detection for the art identified.
            - AzureAiVisionObjectDetectionApproach: Uses AI Vision's Analyze Image API with object detection
                feature enabled to evaluate if art is consistently detected by the service.
        */

        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false);

        IConfiguration config = builder.Build();

        string[] testImages = [
            "https://www.nga.gov/content/dam/ngaweb/visit/cards/card-school-field-trips.jpg",
            "https://www.nga.gov/content/dam/ngaweb/calendar/guided-tours/detail-page/daily/promo-rediscover-tour.jpg",
            "https://www.nga.gov/content/dam/ngaweb/calendar/guided-tours/detail-page/daily/italian-renaissance.jpg",
            "https://www.nga.gov/content/dam/ngaweb/calendar/guided-tours/thumbnails/daily/thumb-17th-dutch-life.jpg",
            "https://www.nga.gov/content/dam/ngaweb/calendar/guided-tours/detail-page/daily/promo-american-stories.jpg",
            "https://www.nga.gov/content/dam/ngaweb/calendar/guided-tours/detail-page/daily/detail-modern-art.jpg",
        ];

        IDetectArt[] approaches = [
            new AzureAiVisionObjectDetectionApproach(config),
            new Gpt4WithVisionOnlyApproach(config),
            new Gpt4WithVisionPlusEnhancementApproach(config),
        ];

        Orchestrator orchestrator = new(approaches);
        foreach (var testImage in testImages) 
        {
            orchestrator.Execute(testImage);
        }
    }
}
