using MTCG.Server.HTTP;
using MTCG.Server.Models;
using System.Text.Json;
using MTCG.Server.Repositories;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class UserService
{
	private UserRepository _userRepository = new UserRepository();
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public Result RegisterUser(Handler handler)
	{
		_logger.Debug("Register User - Registering user...");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Register User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		// TODO: what if its not valid? -> catch exception?
		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		var getUserFromDb = _userRepository.GetUserByUsername(credentials.Username);
		if (getUserFromDb != null)
		{
			_logger.Debug("Register User - User already exists");
			return new Result(false, "User already exists!");
		}
		
		var hashedPassword = Helper.HashPassword(credentials.Password);

		credentials.Password = hashedPassword;

		var registerSuccessful = _userRepository.AddUser(credentials);

		if (registerSuccessful)
		{
			_logger.Debug("Register User - Successfully registered user");
			return new Result(true, "Successfully registered!");
		}
		_logger.Debug("Register User - Registration failed");
		return new Result(false, "Registration failed!");
	}

	public Result LoginUser(Handler handler)
	{
		_logger.Debug("Login User - Logging in user...");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Login User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);

		var userFromDb = _userRepository.GetUserByUsername(credentials.Username);
		if (userFromDb == null)
		{
			_logger.Debug("Login User - Login failed - User does not exist");
			return new Result(false, "Login failed - User does not exist");
		}

		if (!Helper.VerifyPassword(credentials.Password, userFromDb.Password))
		{
			_logger.Debug("Password invalid! - Invalid Password");
			return new Result(false, "Login failed - Login Data not correct");
		}

		_logger.Debug("Password valid! Generating token...");
		userFromDb.Token = Helper.GenerateToken(credentials.Username);
		_logger.Debug("Saving token into DB...");
		var userUpdateSuccessful = _userRepository.UpdateUser(userFromDb);

		if (!userUpdateSuccessful)
		{
			_logger.Debug("Login User - Failed to update user");
			// TODO: maybe add response code to Result - should be internal server error here
			return new Result(true, "Login failed - internal error");
		}

		var temp = new { token = userFromDb.Token };
		var tokenStringified = JsonSerializer.Serialize(temp);

		_logger.Debug("Login User - Successfully logged in user");
		return new Result(true, tokenStringified, Helper.APPL_JSON);
	}

	public UserCredentials? GetAuthorizedUserWithToken(string token)
	{
		return _userRepository.GetUserByToken(token);
	}

	public Result GetUserInformationForUser(Handler handler)
	{
		var userInfo = _userRepository.GetUserInfoByUser(handler.AuthorizedUser);
		if (userInfo == null)
		{
			_logger.Debug("Failed to get user information (null return from db)");
			return new Result(false, "Failed to get user information - no data found");
		}
		_logger.Debug($"Successfully got user information: {JsonSerializer.Serialize(userInfo)}");
		return new Result(true, JsonSerializer.Serialize(userInfo), Helper.APPL_JSON);
	}

	public Result AddOrUpdateUserInfo(Handler handler)
	{
		if (handler.Payload == null)
		{
			return new Result(false, "No payload data found");
		}

		var newUserInfo = JsonSerializer.Deserialize<UserInfo>(handler.Payload);
		newUserInfo.Id = handler.AuthorizedUser.Id;
		var getExistingUserInfo = _userRepository.GetUserInfoByUser(handler.AuthorizedUser);

		if (getExistingUserInfo == null)
		{
			var addUserInfoSuccessful = _userRepository.AddUserInfo(newUserInfo);
			return addUserInfoSuccessful ? new Result(true, JsonSerializer.Serialize(newUserInfo), Helper.APPL_JSON) : new Result(false, "Error while adding info to database");
		}
		
		var updateUserInfoSuccessful = _userRepository.UpdateUserInfo(newUserInfo);
		return updateUserInfoSuccessful ? new Result(true, JsonSerializer.Serialize(newUserInfo), Helper.APPL_JSON) : new Result(false, "Error while adding info to database");
	}

	public Result DeleteUserInfo(Handler handler)
	{
		var existingUserInfo = _userRepository.GetUserInfoByUser(handler.AuthorizedUser);
		if (existingUserInfo == null)
		{
			return new Result(false, "No user info found to delete");
		}
		var userInfoDeleted = _userRepository.RemoveUserInfoByUserId(handler.AuthorizedUser.Id);
		return userInfoDeleted ? new Result(true, "User info successfully deleted") : new Result(false, "Error while deleting user info");
	}
}