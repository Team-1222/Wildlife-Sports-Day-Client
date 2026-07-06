using Newtonsoft.Json;

public class VerifyEmailCodeRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = null!;
    [JsonProperty("code")]
    public string Code { get; set; } = null!;
}