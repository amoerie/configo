namespace Configo.Server;

public static class Routes
{
    public const string Home = "";
    public const string ApiKeys = "api-keys";
    public const string Applications = "applications";
    public static string Schema(string application) => $"applications/{application.ToLowerInvariant()}/schema";
    public const string Tags = "tags";
    public const string Variables = "variables";
}
