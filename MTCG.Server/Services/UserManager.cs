using MTCG.Server.HTTP;
using MTCG.Server.Models;
using System.Text.Json;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class UserManager
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public Result RegisterUser(Handler handler, DatabaseHandler dbHandler)
	{
		_logger.Debug("Register User - Registering user...");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Register User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		// TODO: what if its not valid? -> catch exception?
		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		if (dbHandler.DoesUserExist(credentials.Username))
		{
			_logger.Debug("Register User - User already exists");
			return new Result(false, "User already exists!");
		}
		
		var hashedPassword = Helper.HashPassword(credentials.Password);

		credentials.Password = hashedPassword;

		var registerSuccessful = dbHandler.RegisterUser(credentials);

		if (registerSuccessful)
		{
			_logger.Debug("Register User - Successfully registered user");
			return new Result(true, "Successfully registered!");
		}
		_logger.Debug("Register User - Registration failed");
		return new Result(false, "Registration failed!");
	}

	public Result LoginUser(Handler handler, DatabaseHandler dbHandler)
	{
		_logger.Debug("Login User - Logging in user...");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Login User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);

		var userToken = dbHandler.LoginUser(credentials);

		if (string.IsNullOrEmpty(userToken))
		{
			_logger.Debug("Login User - Login failed");
			return new Result(false, "Login failed!");
		}

		var temp = new { token = userToken };
		var tokenStringified = JsonSerializer.Serialize(temp);

		_logger.Debug("Login User - Successfully logged in user");
		return new Result(true, tokenStringified, Helper.APPL_JSON);
	}
}