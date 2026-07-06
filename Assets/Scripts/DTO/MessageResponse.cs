using Newtonsoft.Json;

public class MessageResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }
}
