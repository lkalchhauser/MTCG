using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface IUserService
{
	public Result RegisterUser(IHandler handler);
	public Result LoginUser(IHandler handler);
	public UserCredentials? GetAuthorizedUserWithToken(string token);
	public Result GetUserInformationForUser(IHandler handler);
	public Result AddOrUpdateUserInfo(IHandler handler);
	public Result DeleteUserInfo(IHandler handler);
	public Result GetUserStats(IHandler handler);
	public Result UpdateUserStats(IHandler handler, UserStats userStats);
	public Result GetScoreboard(IHandler handler);
	public Result UpdatePassword(IHandler handler);
	public bool IsUserAuthorized(IHandler handler);
}