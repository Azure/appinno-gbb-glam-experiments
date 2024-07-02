using ui_backend.Models;

namespace ui_backend.Services {

    public interface IDatabaseService
    {
        Task<IList<ImageMetadata>> Search(float[] embeddings);
    }

}