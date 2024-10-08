using MTCG.Server.HTTP;
using MTCG.Server.Models;
using System.Text.Json;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class UserManager
{

	public Result RegisterUser(Handler handler, DatabaseHandler dbHandler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null) return new Result(false, "Badly formatted data sent!");

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		if (dbHandler.DoesUserExist(credentials.Username))
		{
			return new Result(false, "User already exists!");
		}
		
		var hashedPassword = Helper.HashPassword(credentials.Password);

		credentials.Password = hashedPassword;

		var registerSuccessful = dbHandler.RegisterUser(credentials);

		return !registerSuccessful ? new Result(false, "Registration failed!") : new Result(true, "Successfully registered!");
	}

	public Result LoginUser(Handler handler, DatabaseHandler dbHandler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null) return new Result(false, "Badly formatted data sent!");

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);

		var userToken = dbHandler.LoginUser(credentials);

		if (string.IsNullOrEmpty(userToken)) return new Result(false, "Login failed!");

		var temp = new { token = userToken };
		var tokenStringified = JsonSerializer.Serialize(temp);


		return new Result(true, tokenStringified, Helper.APPL_JSON);
	}
}