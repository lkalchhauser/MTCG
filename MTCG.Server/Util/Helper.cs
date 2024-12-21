using MTCG.Server.HTTP;

namespace MTCG.Server.Util;

using BCrypt.Net;
using MTCG.Server.Services;

public class Helper
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private static UserService _userService = new UserService();
	public static string HashPassword(string password)
	{
		_logger.Debug("Hashing password");
		var hash = BCrypt.HashPassword(password);
		return hash;
	}

	public static bool VerifyPassword(string password, string hash)
	{
		_logger.Debug("Verifying password");
		return BCrypt.Verify(password, hash);
	}

	public static string? GenerateToken(string username)
	{
		_logger.Debug("Generating token");
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

	public static bool IsUserAuthorized(Handler handler)
	{
		var authUser = _userService.GetAuthorizedUserWithToken(handler.GetAuthorizationToken());
		if (authUser == null)
		{
			return false;
		}
		handler.AuthorizedUser = authUser;
		return true;
	}

	public static bool IsRequestedUserAuthorizedUser(Handler handler)
	{
		var username = ExtractUsernameFromPath(handler.Path);
		return handler.AuthorizedUser?.Username == username;
	}

	public static string ExtractUsernameFromPath(string path)
	{
		var split = path.Split("/");
		var username = split[^1];
		return username;
	}

	public const string
		TEXT_PLAIN = "text/plain",
		APPL_JSON = "application/json";

}