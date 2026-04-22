namespace NosVemos.OrquestadorIA.Api.Services;

internal sealed class OpenAiSettings
{
    public bool Enabled { get; set; }
    public bool RequireActivationPassword { get; set; } = true;
    public string Model { get; set; } = "gpt-5.3-codex";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string? ApiKey { get; set; }
    public string? OwnerPassword { get; set; }
    public string? ActivationPassword { get; set; }
}
