namespace Configo.Domain.Models;

public sealed record TagListModel
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required int NumberOfVariables { get; init; }
}
