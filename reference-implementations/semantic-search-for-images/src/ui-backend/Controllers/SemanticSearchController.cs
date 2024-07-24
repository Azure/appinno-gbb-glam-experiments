using Microsoft.AspNetCore.Mvc;
using ui_backend.Models;
using ui_backend.Services;

namespace ui_backend.Controllers
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
        /// <param name="requestBody">The body of the request containing the property "url" set to the URL of the image.</param>
        /// <returns>The search results.</returns>
        [HttpPost("imageUrl")]
        public async Task<ActionResult> SearchByImage([FromBody]ByImageUrlRequest requestBody)
        {
            var embeddings = await _imageService.GenerateImageEmbeddings(requestBody.Url);
            var results = await _databaseService.Search(embeddings);

            return Ok(results);
        }

        /// <summary>
        /// Searches for similar images based on the provided image stream.
        /// </summary>
        /// <returns>The search results.</returns>
        [HttpPost("imageStream")]
        public async Task<IActionResult> SearchByImage()
        {
            _logger.LogInformation("Triggered SearchByImageStream");

            if (Request.ContentType == null)
                return new BadRequestObjectResult("Request Content-Type must be set. If streaming the image as the body, use 'application/octet-stream'. If providing the image as form data, use 'multipart/form-data'");
            if (Request.ContentLength == 0)
                return new BadRequestObjectResult("Request has no content.");

            float[] queryEmbeddings;

            if (Request.ContentType.Contains("application/octet-stream", StringComparison.InvariantCultureIgnoreCase)
                || Request.ContentType.Contains("image/png", StringComparison.InvariantCultureIgnoreCase)
                || Request.ContentType.Contains("image/jpeg", StringComparison.InvariantCultureIgnoreCase)
                || Request.ContentType.Contains("image/gif", StringComparison.InvariantCultureIgnoreCase))
            {
                var sanitizedContentType = Request.ContentType.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
                _logger.LogInformation($"Content-Type: {sanitizedContentType}. Passing request body directly to generate embeddings.");
                // Assume the body is an image stream and directly pass along...
                queryEmbeddings = await _imageService.GenerateImageEmbeddings(Request.Body);
            }
            else if (Request.ContentType.Contains("multipart/form-data", StringComparison.InvariantCultureIgnoreCase))
            {
                if (Request.Form.Files.Count == 0)
                    return new BadRequestObjectResult("Request form data does not include a file.");
                if (Request.Form.Files.Count > 1)
                    return new BadRequestObjectResult("Request form data includes more than one file; only one may be accepted.");

                _logger.LogInformation("Content-Type: multipart/form-data. Passing request form file read stream to generate embeddings.");

                var file = Request.Form.Files[0];
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
        /// <param name="requestBody">The body of the request containing the property "text" set to the text to search for.</param>
        /// <returns>The search results.</returns>
        [HttpPost("text")]
        public async Task<IActionResult> SearchByText([FromBody]ByTextRequest requestBody)
        {
            var embeddings = await _imageService.GenerateTextEmbeddings(requestBody.Text);
            return Ok(await _databaseService.Search(embeddings));
        }
    }
}
