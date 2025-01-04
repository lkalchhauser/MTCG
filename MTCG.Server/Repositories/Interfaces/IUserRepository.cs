using MTCG.Server.HTTP;
using MTCG.Server.Models;

namespace MTCG.Server.Repositories.Interfaces;

public interface IUserRepository
{
	public UserCredentials? GetUserByUsername(string username);
	public UserCredentials? GetUserById(int id);
	public UserCredentials? GetUserByToken(string token);
	public int AddUser(UserCredentials user);
	public bool UpdateUser(UserCredentials user);
	public bool RemoveUser(int userId);
	public UserInfo? GetUserInfoByUser(UserCredentials user);
	public bool AddUserInfo(UserInfo userInfo);
	public bool UpdateUserInfo(UserInfo userInfo);
	public bool RemoveUserInfoByUserId(int userId);
	public UserStats? GetUserStats(IHandler handler);
	public bool AddUserStats(UserStats userStats);
	public bool UpdateUserStats(UserStats userStats);
	public List<UserStats> GetAllStats();
}