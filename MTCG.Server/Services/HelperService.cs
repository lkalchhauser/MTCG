using MTCG.Server.HTTP;
using MTCG.Server.Models;

namespace MTCG.Server.Services;

using BCrypt.Net;
using MTCG.Server.Services.Interfaces;

/**
 *	Helper service providing various helper methods
 */
public class HelperService : IHelperService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 *	Hashes a password using BCrypt and returns it
	 *	<param name="password">the password to hash</param>
	 *	<returns>the hashed password</returns>
	 */
	public string HashPassword(string password)
	{
		_logger.Debug("Hashing password");
		var hash = BCrypt.HashPassword(password);
		return hash;
	}

	/**
	 *	Verifies a password against a hash
	 *	<param name="password">the password to verify</param>
	 *	<param name="hash">the hash to verify against</param>
	 *	<returns>true if the password matches the hash, false otherwise</returns>
	 */
	public bool VerifyPassword(string password, string hash)
	{
		_logger.Debug("Verifying password");
		return BCrypt.Verify(password, hash);
	}

	/**
	 *	Generates a token for a user
	 *	<param name="username">the username to generate the token for</param>
	 *	<returns>the generated token</returns>
	 */
	public string? GenerateToken(string username)
	{
		_logger.Debug("Generating token");
		//TODO make this better (for test script it has to stay like this)
		return $"{username}-mtcgToken";
	}

	/**
	 *	HTTP status codes
	 */
	public Dictionary<int, string> HTTP_CODES = new Dictionary<int, string>() {
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

	public Dictionary<int, string> GetHttpCodes()
	{
		return HTTP_CODES;
	}

	/**
	 *	Checks if the requested user is the authorized user
	 *	<param name="handler">The handler containing the authorized user</param>
	 *	<returns>true if the requested user is the authorized user, false otherwise</returns>
	 */
	public bool IsRequestedUserAuthorizedUser(IHandler handler)
	{
		var username = ExtractUsernameFromPath(handler.Path);
		return handler.AuthorizedUser?.Username == username;
	}

	/**
	 *	Extracts the username from a path
	 *	<param name="path">The path to extract the username from</param>
	 *	<returns>The extracted username</returns>
	 */
	public string ExtractUsernameFromPath(string path)
	{
		var split = path.Split("/");
		var username = split[^1];
		return username;
	}

	public const string
		TEXT_PLAIN = "text/plain",
		APPL_JSON = "application/json";

	/**
	 *	Generates a formatted scoreboard table based on the sorted scoreboard users
	 * <param name="sortedScoreboardUsers">the sorted (by elo) scoreboard users</param>
	 *	<returns>The formatted scoreboard table as a string</returns>
	 */
	public string GenerateScoreboardTable(List<ScoreboardUser> sortedScoreboardUsers)
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

	/**
	 *	Parses an enum value from a string or returns null if parsing fails
	 *	<param name="value">The value to parse</param>
	 *	<returns>The parsed enum value or null if parsing fails</returns>
	 */
	public TEnum? ParseEnumOrNull<TEnum>(string value) where TEnum : struct, Enum
	{
		// Use TryParse to attempt parsing
		if (Enum.TryParse(value, true, out TEnum result))
		{
			return result; // Return the parsed enum value
		}
		return null; // Return null if parsing fails
	}
}