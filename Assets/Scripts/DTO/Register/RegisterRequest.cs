using Newtonsoft.Json;

public class RegisterRequest
{
    [JsonProperty("email")]
    public string Email { get; set; } = null!;
    [JsonProperty("nickname")]
    public string Nickname { get; set; } = null!;
    [JsonProperty("password")]
    public string Password { get; set; } = null!;
    [JsonProperty("confirmPassword")]
    public string ConfirmPassword { get; set; } = null!;
}