using Microsoft.AspNetCore.Mvc;
using ui_backend.Services;

namespace MyApp.Namespace
{
    /// <summary>
    /// Controller for performing semantic search operations.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SemanticSearchController : ControllerBase
    {
        private readonly IDatabaseService _databaseService;
        private readonly IImageService _imageService;
        private readonly ILogger<SemanticSearchController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticSearchController"/> class.
        /// </summary>
        /// <param name="databaseService">The database service.</param>
        /// <param name="imageService">The image service.</param>
        /// <param name="logger">The logger.</param>
        public SemanticSearchController(IDatabaseService databaseService, IImageService imageService, ILogger<SemanticSearchController> logger)
        {
            _databaseService = databaseService;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>
        /// Searches for similar images based on the provided image URL.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        /// <returns>The search results.</returns>
        [HttpPost("imageUrl")]
        public async Task<ActionResult> SearchByImage(string url)
        {
            var embeddings = await _imageService.GenerateImageEmbeddings(url);
            var results = await _databaseService.Search(embeddings);

            return Ok(results);
        }

        /// <summary>
        /// Searches for similar images based on the provided image stream.
        /// </summary>
        /// <param name="req">The HTTP request containing the image stream.</param>
        /// <returns>The search results.</returns>
        [HttpPost("imageStream")]
        public async Task<IActionResult> SearchByImage(HttpRequest req)
        {
            _logger.LogInformation("Triggered SearchByImageStream");

            if (req.ContentType == null)
                return new BadRequestObjectResult("Request Content-Type must be set. If streaming the image as the body, use 'application/octet-stream'. If providing the image as form data, use 'multipart/form-data'");
            if (req.ContentLength == 0)
                return new BadRequestObjectResult("Request has no content.");

            float[] queryEmbeddings;

            if (req.ContentType.Contains("application/octet-stream", StringComparison.InvariantCultureIgnoreCase)
                || req.ContentType.Contains("image/png", StringComparison.InvariantCultureIgnoreCase)
                || req.ContentType.Contains("image/jpeg", StringComparison.InvariantCultureIgnoreCase)
                || req.ContentType.Contains("image/gif", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation($"Content-Type: {req.ContentType}. Passing request body directly to generate embeddings.");
                // Assume the body is an image stream and directly pass along...
                queryEmbeddings = await _imageService.GenerateImageEmbeddings(req.Body);
            }
            else if (req.ContentType.Contains("multipart/form-data", StringComparison.InvariantCultureIgnoreCase))
            {
                if (req.Form.Files.Count == 0)
                    return new BadRequestObjectResult("Request form data does not include a file.");
                if (req.Form.Files.Count > 1)
                    return new BadRequestObjectResult("Request form data includes more than one file; only one may be accepted.");

                _logger.LogInformation("Content-Type: multipart/form-data. Passing request form file read stream to generate embeddings.");

                var file = req.Form.Files[0];
                queryEmbeddings = await _imageService.GenerateImageEmbeddings(file.OpenReadStream());
            }
            else
            {
                return new BadRequestObjectResult("Request could not be handled. Expects either image as form data (multipart/form-data) or direct body stream (appliation/octet-stream).");
            }

            return Ok(await _databaseService.Search(queryEmbeddings));
        }

        /// <summary>
        /// Searches for similar images based on the provided text.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <returns>The search results.</returns>
        /// [HttpPost("text")]
        public async Task<IActionResult> SearchByText(string text)
        {
            var embeddings = await _imageService.GenerateTextEmbeddings(text);
            return Ok(await _databaseService.Search(embeddings));
        }
    }
}
