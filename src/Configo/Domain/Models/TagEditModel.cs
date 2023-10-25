using System.ComponentModel.DataAnnotations;

namespace Configo.Domain.Models;

public sealed class TagEditModel
{
    public int? Id { get; set; }
    
    [Required]
    [MaxLength(256)]
    public string? Name { get; set; }
}
