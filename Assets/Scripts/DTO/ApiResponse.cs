using Newtonsoft.Json;


public class ApiResponse<T>
{
    [JsonProperty("success")]
    public  bool Success { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("data")]
    public T? Data { get; set; }
}
