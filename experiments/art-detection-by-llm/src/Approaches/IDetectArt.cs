namespace ArtDetection.Approaches;

public interface IDetectArt
{
    public string ApproachShortName { get; }
    public SixLabors.ImageSharp.Drawing.Polygon[] GetBoundingBoxes(string imageUrl);
}