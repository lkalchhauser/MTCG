using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

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

	public static string GenerateScoreboardTable(List<ScoreboardUser> sortedScoreboardUsers)
	{
		var headers = new[] { "Username", "Elo", "Wins", "Losses", "Draws" };

		var usernameWidth = Math.Max(headers[0].Length, sortedScoreboardUsers.Max(u => u.Username.Length));
		var eloWidth = Math.Max(headers[1].Length, sortedScoreboardUsers.Max(u => u.Elo.ToString().Length));
		var winsWidth = Math.Max(headers[2].Length, sortedScoreboardUsers.Max(u => u.Wins.ToString().Length));
		var lossesWidth = Math.Max(headers[3].Length, sortedScoreboardUsers.Max(u => u.Losses.ToString().Length));
		var drawsWidth = Math.Max(headers[4].Length, sortedScoreboardUsers.Max(u => u.Draws.ToString().Length));

		var headerRow = $"{headers[0].PadRight(usernameWidth)} | {headers[1].PadRight(eloWidth)} | {headers[2].PadRight(winsWidth)} | {headers[3].PadRight(lossesWidth)} | {headers[4].PadRight(drawsWidth)}";
		var separatorRow = new string('-', headerRow.Length);

		var rows = sortedScoreboardUsers.Select(u =>
			$"{u.Username.PadRight(usernameWidth)} | {u.Elo.ToString().PadRight(eloWidth)} | {u.Wins.ToString().PadRight(winsWidth)} | {u.Losses.ToString().PadRight(lossesWidth)} | {u.Draws.ToString().PadRight(drawsWidth)}"
		);

		var finalTable = $"{headerRow}\n{separatorRow}\n{string.Join("\n", rows)}";
		return finalTable;
	}

	public static TEnum? ParseEnumOrNull<TEnum>(string value) where TEnum : struct, Enum
	{
		// Use TryParse to attempt parsing
		if (Enum.TryParse(value, true, out TEnum result))
		{
			return result; // Return the parsed enum value
		}
		return null; // Return null if parsing fails
	}
}