using MTCG.Server.HTTP;
using MTCG.Server.Models;
using System.Text.Json;

namespace MTCG.Server.Services;

public class UserManager
{
	private readonly DatabaseHandler _dbHandler = DatabaseHandler.Instance;

	public bool RegisterUser(Handler handler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null) return false;

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		var registerSuccessful = _dbHandler.RegisterUser(credentials);
		return registerSuccessful;
	}

	public string LoginUser(Handler handler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null) return "";

		var credentials = JsonSerializer.Deserialize<UserCredentials>(handler.Payload);
		var userToken = _dbHandler.LoginUser(credentials);

		return string.IsNullOrEmpty(userToken) ? "" : userToken;
	}
}