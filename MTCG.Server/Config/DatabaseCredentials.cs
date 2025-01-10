namespace MTCG.Server.Config;

public static class DatabaseCredentials
{
	public static string? DbHost => Environment.GetEnvironmentVariable("MTCG_DB_HOST");
	public static string? DbPort => Environment.GetEnvironmentVariable("MTCG_DB_PORT");
	public static string? DbUser => Environment.GetEnvironmentVariable("MTCG_DB_USER");
	public static string? DbPassword = Environment.GetEnvironmentVariable("MTCG_DB_PASSWORD");
	public static string? DbName = Environment.GetEnvironmentVariable("MTCG_DB_NAME");

	public static string GetConnectionString()
	{
		return $"Host={DbHost};Port={DbPort};Username={DbUser};Password={DbPassword};Database={DbName}";
	}
}