namespace Configo.Server;

public static class Routes
{
    public const string Home = "";
    public const string ApiKeys = "api-keys";
    public const string Applications = "applications";
    public static string Schema(string application) => $"applications/{application}/schema";
    public const string TagGroups = "tag-groups";
    public const string Variables = "variables";
    public static string Tags(string group) => $"tags/{group.ToLowerInvariant()}";
}
