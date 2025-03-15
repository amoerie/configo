using System.ComponentModel.DataAnnotations;

namespace Configo.Server;

public sealed record ConfigoAuthenticationOptions
{
    public const string SectionName = "Authentication";
    
    [Required]
    public required MicrosoftOptions Microsoft { get; set; }
}

public sealed record MicrosoftOptions
{
    [Required]
    public required string ClientId { get; set; }
    
    [Required]
    public required string ClientSecret { get; set; }
}
