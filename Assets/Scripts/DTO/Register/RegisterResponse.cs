using Newtonsoft.Json;
using System;

[Serializable]
public class RegisterResponse
{
    [JsonProperty("userId")]
    public int UserId { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }
}