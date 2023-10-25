using System.ComponentModel.DataAnnotations;

namespace Configo.Domain.Models;

public sealed class TagDeleteModel
{
    [Required]
    public int? Id { get; set; }
}
