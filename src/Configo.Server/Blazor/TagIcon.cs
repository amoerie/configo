using System.Reflection;
using MudBlazor;

namespace Configo.Server.Blazor;

public sealed record TagIcon(string Name, string Value)
{
    public static readonly TagIcon[] Icons = typeof(Icons.Material.Filled)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(fi => fi is { IsLiteral: true,IsInitOnly: false } && fi.FieldType == typeof(string))
        .Select(x => new { Name = x.Name, Value = x.GetRawConstantValue() as string })
        .Where(x => x.Value != null)
        .Select(x => new TagIcon(x.Name, x.Value!))
        .ToArray();

    public static readonly TagIcon Default = new TagIcon(nameof(MudBlazor.Icons.Material.Filled.QuestionMark), MudBlazor.Icons.Material.Filled.QuestionMark);
    
    public static readonly IDictionary<string, TagIcon> IconsByName = Icons.DistinctBy(i => i.Name).ToDictionary(i => i.Name);

    public static TagIcon GetByName(string name) => IconsByName.TryGetValue(name, out var tagIcon) ? tagIcon : Default;

    public static Task<IEnumerable<TagIcon>> SearchAsync(string name) => Task.FromResult(Search(name));
    public static IEnumerable<TagIcon> Search(string name) => Icons.Where(i => i.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
}
