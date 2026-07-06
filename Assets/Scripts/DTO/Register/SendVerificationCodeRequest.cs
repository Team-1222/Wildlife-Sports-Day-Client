using Newtonsoft.Json;

public class SendVerificationCodeRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = null!;
}
