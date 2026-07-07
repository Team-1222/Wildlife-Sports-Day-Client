using Newtonsoft.Json;
using System;

[Serializable]
public class MessageResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }
}
