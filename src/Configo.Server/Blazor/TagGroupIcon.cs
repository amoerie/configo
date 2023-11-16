using System.Reflection;
using MudBlazor;

namespace Configo.Server.Blazor;

public sealed record TagGroupIcon(string Name, string Value)
{
    public static readonly TagGroupIcon[] Icons = typeof(Icons.Material.Filled)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(fi => fi is { IsLiteral: true,IsInitOnly: false } && fi.FieldType == typeof(string))
        .Select(x => new { Name = x.Name, Value = x.GetRawConstantValue() as string })
        .Where(x => x.Value != null)
        .Select(x => new TagGroupIcon(x.Name, x.Value!))
        .ToArray();

    public static readonly TagGroupIcon Default = new TagGroupIcon(nameof(MudBlazor.Icons.Material.Filled.QuestionMark), MudBlazor.Icons.Material.Filled.QuestionMark);
    
    public static readonly IDictionary<string, TagGroupIcon> IconsByName = Icons.DistinctBy(i => i.Name).ToDictionary(i => i.Name);

    public static TagGroupIcon GetByName(string name) => IconsByName.TryGetValue(name, out var tagIcon) ? tagIcon : Default;

    public static Task<IEnumerable<TagGroupIcon>> SearchAsync(string name) => Task.FromResult(Search(name));
    public static IEnumerable<TagGroupIcon> Search(string name) => Icons.Where(i => i.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
}
