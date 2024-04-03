namespace ArtDetection.Models.Gpt4WithVisionOnly;

#region Prompt-defined expected response format

public class BoundingBox
{
    public int x { get; set; }
    public int y { get; set; }
}

public class PromptDefinedResponse
{
    public List<BoundingBox> bounding_box { get; set; }

    public override string ToString()
    {
        var output = new System.Text.StringBuilder("[ ");
        foreach (var box in bounding_box)
        {
            output.Append('{');
            output.Append($"X: {box.x}, Y: {box.y}");
            output.Append("} ");
        }
        output.Append(']');
        return output.ToString();
    }
}

#endregion