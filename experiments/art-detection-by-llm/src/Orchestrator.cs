using ArtDetection.Approaches;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace ArtDetection;

public class Orchestrator(IDetectArt[] approaches)
{
    private readonly IDetectArt[] _approaches = approaches;
    private readonly HttpClient _httpClient = new();

    public void Execute(string originalImageUrl) 
    {
        var fileName = Path.GetFileName(originalImageUrl);

        foreach (var approach in _approaches)
        {
            Console.WriteLine("\n\n====================================");
            Console.WriteLine($"ORIGINAL: {fileName}");
            Console.WriteLine($"APPROACH: {approach.ApproachShortName}");
            Console.WriteLine("====================================\n");

            // Get the original image so that we can see what the approach has found
            var imageResponse = _httpClient.GetAsync(originalImageUrl).Result;
            var imageStream = imageResponse.Content.ReadAsStream();
            var originalImage = Image.Load(imageStream);

            // Get the approach's bounding boxes and draw them
            var boundingBoxes = approach.GetBoundingBoxes(originalImageUrl);
            foreach (var polygon in boundingBoxes)
            {
                // Create a random identifier to help align
                var randomIdentifier = Guid.NewGuid();
                Console.WriteLine($"Processing {randomIdentifier}: [ top: {polygon.Bounds.Top}, left: {polygon.Bounds.Left}, width: {polygon.Bounds.Width}, height: {polygon.Bounds.Height} ]");

                try
                {
                    // Draw the bounding box on the original image
                    var pen = GetNextPen();
                    originalImage.Mutate(x => x.Draw(pen, polygon));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"START ERROR: Couldn't process bounding box for {randomIdentifier}.");
                    Console.WriteLine($"\tMessage: {e.Message}, Stack Trace: {e.StackTrace}");
                    Console.WriteLine("END ERROR --------------------");
                }
            }

            // Save the annotated original image
            originalImage.SaveAsJpeg($"output/{approach.ApproachShortName}-annotated-{fileName}");
        }
    }

    private int _requestCountForPen = 0;
    private SolidPen GetNextPen()
    {
        Color[] colors = [
            Color.Red,
            Color.Orange,
            Color.Yellow,
            Color.Green,
            Color.Blue,
            Color.Indigo,
            Color.Violet
        ];
        if (_requestCountForPen == colors.Length)
            _requestCountForPen = 0;

        return Pens.Solid(colors[_requestCountForPen++], 1);
    }

}