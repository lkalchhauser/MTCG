using MTCG.Server.HTTP;
using MTCG.Server.Models;
using System.Text.Json;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class UserService(IUserRepository userRepository, IHelperService helperService)
	: IUserService
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public Result RegisterUser(IHandler handler)
	{
		_logger.Debug("Register User - Registering user...");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Register User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!", statusCode: 400);
		}

		// TODO: what if its not valid? -> catch exception?
		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		var getUserFromDb = userRepository.GetUserByUsername(credentials.Username);
		if (getUserFromDb != null)
		{
			_logger.Debug("Register User - User already exists");
			return new Result(false, "User already exists!", statusCode: 409);
		}
		
		var hashedPassword = helperService.HashPassword(credentials.Password);

		credentials.Password = hashedPassword;

		var registerSuccessful = userRepository.AddUser(credentials);

		if (registerSuccessful != 0)
		{
			_logger.Debug("Register User - Successfully registered user");
			_logger.Debug("Adding default user stats");
			// in theory, we could just add the id since it has default values but i like doing it this way
			var addUserStatsSuccessful = userRepository.AddUserStats(new UserStats()
			{
				Id = registerSuccessful,
				Elo = 100,
				Draws = 0,
				Losses = 0,
				Wins = 0
			});

			if (addUserStatsSuccessful) return new Result(true, "Successfully registered!", statusCode: 201);

			_logger.Debug("Register User - Failed to add default user stats");
			return new Result(false, "Failed to add default user stats", statusCode: 400);

		}
		_logger.Debug("Register User - Registration failed");
		return new Result(false, "Registration failed!", statusCode: 400);
	}

	public Result LoginUser(IHandler handler)
	{
		_logger.Debug("Login User - Logging in user...");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Login User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!", statusCode: 400);
		}

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);

		var userFromDb = userRepository.GetUserByUsername(credentials.Username);
		if (userFromDb == null)
		{
			_logger.Debug("Login User - Login failed - User does not exist");
			return new Result(false, "Login failed - User does not exist", statusCode: 404);
		}

		if (!helperService.VerifyPassword(credentials.Password, userFromDb.Password))
		{
			_logger.Debug("Password invalid! - Invalid Password");
			return new Result(false, "Login failed - Login Data not correct", statusCode: 401);
		}

		_logger.Debug("Password valid! Generating token...");
		userFromDb.Token = helperService.GenerateToken(credentials.Username);
		_logger.Debug("Saving token into DB...");
		var userUpdateSuccessful = userRepository.UpdateUser(userFromDb);

		if (!userUpdateSuccessful)
		{
			_logger.Debug("Login User - Failed to update user");
			return new Result(true, "Login failed - internal error", statusCode: 400);
		}

		var temp = new { token = userFromDb.Token };
		var tokenStringified = JsonSerializer.Serialize(temp);

		_logger.Debug("Login User - Successfully logged in user");
		return new Result(true, tokenStringified, HelperService.APPL_JSON, statusCode: 200);
	}

	public UserCredentials? GetAuthorizedUserWithToken(string token)
	{
		return userRepository.GetUserByToken(token);
	}

	public Result GetUserInformationForUser(IHandler handler)
	{
		var userInfo = userRepository.GetUserInfoByUser(handler.AuthorizedUser);
		if (userInfo == null)
		{
			_logger.Debug("Failed to get user information (null return from db)");
			return new Result(false, "Failed to get user information - no data found", statusCode: 404);
		}
		_logger.Debug($"Successfully got user information: {JsonSerializer.Serialize(userInfo)}");
		return new Result(true, JsonSerializer.Serialize(userInfo), HelperService.APPL_JSON, statusCode: 200);
	}

	public bool IsUserAuthorized(IHandler handler)
	{
		var authUser = GetAuthorizedUserWithToken(handler.GetAuthorizationToken());
		if (authUser == null)
		{
			return false;
		}
		handler.AuthorizedUser = authUser;
		return true;
	}

	public Result AddOrUpdateUserInfo(IHandler handler)
	{
		if (handler.Payload == null)
		{
			return new Result(false, "No payload data found", statusCode: 400);
		}

		var newUserInfo = JsonSerializer.Deserialize<UserInfo>(handler.Payload);
		newUserInfo.Id = handler.AuthorizedUser.Id;
		var getExistingUserInfo = userRepository.GetUserInfoByUser(handler.AuthorizedUser);

		if (getExistingUserInfo == null)
		{
			var addUserInfoSuccessful = userRepository.AddUserInfo(newUserInfo);
			return addUserInfoSuccessful ? new Result(true, JsonSerializer.Serialize(newUserInfo), HelperService.APPL_JSON, statusCode: 201) : new Result(false, "Error while adding info to database", statusCode: 400);
		}
		
		var updateUserInfoSuccessful = userRepository.UpdateUserInfo(newUserInfo);
		return updateUserInfoSuccessful ? new Result(true, JsonSerializer.Serialize(newUserInfo), HelperService.APPL_JSON, statusCode: 200) : new Result(false, "Error while adding info to database", statusCode: 400);
	}

	public Result DeleteUserInfo(IHandler handler)
	{
		var existingUserInfo = userRepository.GetUserInfoByUser(handler.AuthorizedUser);
		if (existingUserInfo == null)
		{
			return new Result(false, "No user info found to delete", statusCode: 404);
		}
		var userInfoDeleted = userRepository.RemoveUserInfoByUserId(handler.AuthorizedUser.Id);
		return userInfoDeleted ? new Result(true, "User info successfully deleted", statusCode: 200) : new Result(false, "Error while deleting user info", statusCode: 500);
	}

	public Result GetUserStats(IHandler handler)
	{
		var userStats = userRepository.GetUserStats(handler);
		return userStats == null ? new Result(false, "No user stats found", statusCode: 404) : new Result(true, JsonSerializer.Serialize(userStats), HelperService.APPL_JSON, statusCode: 200);
	}

	public Result UpdateUserStats(IHandler handler, UserStats userStats)
	{
		var updateSuccessful = userRepository.UpdateUserStats(userStats);
		return updateSuccessful ? new Result(true, "User stats successfully updated") : new Result(false, "Error while updating user stats");
	}

	public Result GetScoreboard(IHandler handler)
	{
		var allStats = userRepository.GetAllStats();
		if (allStats.Count == 0)
		{
			return new Result(true, "No stats found", statusCode: 404);
		}

		List<ScoreboardUser> scoreboard = [];

		foreach (var stat in allStats)
		{
			var user = userRepository.GetUserById(stat.Id);
			if (user == null)
			{
				return new Result(false, "Error while getting user data", statusCode: 500);
			}
			scoreboard.Add(new ScoreboardUser()
			{
				Id = stat.Id,
				Username = user.Username,
				Elo = stat.Elo,
				Wins = stat.Wins,
				Losses = stat.Losses,
				Draws = stat.Draws
			});
		}

		// in theory i could sort the list when getting it from the db but i like doing it like this
		var sortedScoreboardUsers = scoreboard.OrderByDescending(scoreboardUser => scoreboardUser.Elo).ToList();

		if (handler.HasPlainFormat())
		{
			var finalText = helperService.GenerateScoreboardTable(sortedScoreboardUsers);
			return new Result(true, finalText, HelperService.TEXT_PLAIN, 200);
		}

		return new Result(true, JsonSerializer.Serialize(sortedScoreboardUsers), HelperService.APPL_JSON, 200);
	}

	public Result UpdatePassword(IHandler handler)
	{
		_logger.Debug($"Updating password for user {handler.AuthorizedUser.Username}");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Register User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!", statusCode: 400);
		}

		// TODO: what if its not valid? -> catch exception?
		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		var getUserFromDb = userRepository.GetUserById(handler.AuthorizedUser.Id);
		getUserFromDb.Password = helperService.HashPassword(credentials.Password);
		getUserFromDb.Token = "";
		userRepository.UpdateUser(getUserFromDb);
		return new Result(true, "Password successfully updated", statusCode: 200);
	}
}