namespace MTCG.Server.Util;

using BCrypt.Net;

public class Helper
{
	public static string HashPassword(string password)
	{
		var hash = BCrypt.HashPassword(password);
		var isValid = BCrypt.Verify(password, hash);
		//TODO
		return hash;
	}

	public static bool VerifyPassword(string password, string hash)
	{
		return BCrypt.Verify(password, hash);
	}

	public static bool ValidUserCredentials(string password, string userName)
	{
		// TODO:
		return true;
	}

	public static string GenerateToken(string username)
	{
		//TODO make this better (for test script it has to stay like this)
		return $"{username}-mtcgToken";
	}

	public static Dictionary<int, string> HTTP_CODES = new Dictionary<int, string>() {
		{200, "OK"},
		{201, "Created"},
		{202, "Accepted"},
		{204, "No Content"},
		{400, "Bad Request"},
		{401, "Unauthorized"},
		{403, "Forbidden"},
		{404, "Not Found"},
		{405, "Method Not Allowed"},
		{409, "Conflict"},
		{500, "Internal Server Error"}
	};

	public const string
		TEXT_PLAIN = "text/plain",
		APPL_JSON = "application/json";

}