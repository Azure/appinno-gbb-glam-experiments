namespace ArtDetection.Models;

public class Choice
{
    public string finish_reason { get; set; }
    public int index { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public CCVR_Message message { get; set; }
    public ContentFilterResults content_filter_results { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("enhancements")]
    public CCVR_Enhancements enhancements { get; set; }
}

public class ContentFilterResults
{
    public Hate hate { get; set; }
    public SelfHarm self_harm { get; set; }
    public Sexual sexual { get; set; }
    public Violence violence { get; set; }
}

public class CCVR_Enhancements
{
    [System.Text.Json.Serialization.JsonPropertyName("grounding")]
    public CCVR_Grounding grounding { get; set; }
}

public class CCVR_Grounding
{
    public List<Line> lines { get; set; }
    public string status { get; set; }
}

public class Hate
{
    public bool filtered { get; set; }
    public string severity { get; set; }
}

public class Line
{
    public string text { get; set; }
    public List<Span> spans { get; set; }
}

public class CCVR_Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class CCVR_Polygon
{
    public double x { get; set; }
    public double y { get; set; }
}

public class PromptFilterResult
{
    public int prompt_index { get; set; }
    public ContentFilterResults content_filter_results { get; set; }
}

public class ChatCompletionWithVisionResponse
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<PromptFilterResult> prompt_filter_results { get; set; }
    public List<Choice> choices { get; set; }
    public Usage usage { get; set; }
}

public class SelfHarm
{
    public bool filtered { get; set; }
    public string severity { get; set; }
}

public class Sexual
{
    public bool filtered { get; set; }
    public string severity { get; set; }
}

public class Span
{
    public string text { get; set; }
    public int length { get; set; }
    public int offset { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("polygon")]
    public List<CCVR_Polygon> polygon { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}

public class Violence
{
    public bool filtered { get; set; }
    public string severity { get; set; }
}
