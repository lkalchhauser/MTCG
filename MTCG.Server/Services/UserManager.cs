using MTCG.Server.HTTP;
using MTCG.Server.Models;
using System.Text.Json;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class UserManager
{
	private readonly DatabaseHandler _dbHandler = DatabaseHandler.Instance;

	public Result RegisterUser(Handler handler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null) return new Result(false, "Badly formatted data sent!");

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		if (_dbHandler.DoesUserExist(credentials.Username))
		{
			return new Result(false, "Username already exists!");
		}
		var registerSuccessful = _dbHandler.RegisterUser(credentials);

		return !registerSuccessful ? new Result(false, "Registration failed!") : new Result(true, "Successfully registered!");
	}

	public Result LoginUser(Handler handler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null) return new Result(false, "Badly formatted data sent!");

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		var userToken = _dbHandler.LoginUser(credentials);

		if (string.IsNullOrEmpty(userToken))
		{
			return new Result(false, "Login failed!");
		}
		else
		{
			return new Result(true, userToken);
		}
	}
}