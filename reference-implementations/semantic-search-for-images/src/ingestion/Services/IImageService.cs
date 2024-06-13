namespace ingestion.Services
{
    public interface IImageService
    {
        Task<Stream> DownloadImage(string imageUrl);
        Task<float[]> GenerateImageEmbeddings(Stream imageStream);
    }
}