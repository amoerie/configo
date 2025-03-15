namespace Configo.Server;

public sealed record AuthenticationOptions
{
    public required MicrosoftOptions Microsoft { get; set; }
}

public sealed record MicrosoftOptions
{
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}
