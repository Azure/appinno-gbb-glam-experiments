namespace ui_backend.Services
{
    public interface IImageService
    {
        Task<float[]> GenerateImageEmbeddings(Stream imageStream);
        Task<float[]> GenerateImageEmbeddings(string imageUrl);
        Task<float[]> GenerateTextEmbeddings(string text);
    }
}