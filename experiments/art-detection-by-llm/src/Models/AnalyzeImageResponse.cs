namespace ArtDetection.Models;

public class AnalyzeImageResponse
{
    public List<Objects> objects { get; set; }
}

public class Objects
{
    public Rectangle rectangle { get; set; }
    public string @object { get; set; }
    public double confidence { get; set; }
}

public class Rectangle
{
    public int x { get; set; }
    public int y { get; set; }
    public int w { get; set; }
    public int h { get; set; }
}