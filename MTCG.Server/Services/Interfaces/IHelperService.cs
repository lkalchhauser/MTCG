using MTCG.Server.HTTP;
using MTCG.Server.Models;

namespace MTCG.Server.Services.Interfaces;

public interface IHelperService
{
	public string HashPassword(string password);
	public bool VerifyPassword(string password, string hash);
	public string? GenerateToken(string username);
	public bool IsRequestedUserAuthorizedUser(IHandler handler);
	public string ExtractUsernameFromPath(string path);
	public string GenerateScoreboardTable(List<ScoreboardUser> sortedScoreboardUsers);
	public TEnum? ParseEnumOrNull<TEnum>(string value) where TEnum : struct, Enum;
	Dictionary<int, string> GetHttpCodes();
}