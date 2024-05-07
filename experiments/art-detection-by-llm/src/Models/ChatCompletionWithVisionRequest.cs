namespace ArtDetection.Models;

public class DataSource
{
    public string type { get; set; }
    public Parameters parameters { get; set; }
}

public class Enhancements
{
    public Ocr ocr { get; set; }
    public Grounding grounding { get; set; }
}

public class Grounding
{
    public bool enabled { get; set; }
}

public class Message
{
    public string role { get; set; }
    public object content { get; set; }
}

public class Ocr
{
    public bool enabled { get; set; }
}

public class Parameters
{
    public string endpoint { get; set; }
    public string key { get; set; }
}

public class ChatCompletionWithVisionRequest
{
    public Enhancements enhancements { get; set; }
    public List<DataSource> dataSources { get; set; }
    public List<Message> messages { get; set; }
    public int max_tokens { get; set; }
    public bool stream { get; set; }
}

