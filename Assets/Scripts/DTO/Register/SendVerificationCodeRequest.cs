using Newtonsoft.Json;
using System;

[Serializable]
public class SendVerificationCodeRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = null!;
}
