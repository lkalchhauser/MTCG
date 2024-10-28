using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MTCG.Server.Models;
using MTCG.Server.Services;

namespace MTCG.Server.HTTP;

public class Router
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly DatabaseHandler _dbHandler = DatabaseHandler.Instance;
	private UserManager _userManager = new UserManager();

	public Router()
	{
		_dbHandler.SetupDbConnection();
	}

	public async void HandleIncoming(Handler handler)
	{
		// COULD CHANGE THIS TO DIFFERENT ENDPOINTS
		switch (handler.Method)
		{
			case "GET":
				switch(handler.Path)
				{
					case "/":
						_logger.Debug("Routing GET /");
						handler.Reply(200, "Welcome to the Monster Trading Card Game Server!");
						break;
					case "/users/username": // TODO: change to include
						// return user with username
						break;
					case "/cards":
						// return all cards
						break;
					case "/deck":
						// return deck of user
						break;
					case "/stats":
						// return stats of user
						break;
					case "/scoreboard":
						// return scoreboard
						break;
					case "/tradings":
						// return all tradings
						break;
					default:
						handler.Reply(404);
						break;
				}
				// users/username
				// cards
				// deck
				// stats
				// scoreboard
				// tradings
				break;
			case "POST":
				switch (handler.Path)
				{
					case "/users":
						_logger.Debug("Routing POST /user");
						var userRegisterResult = _userManager.RegisterUser(handler, _dbHandler);
						handler.Reply(userRegisterResult.Success ? 201 : 400, userRegisterResult.Message, userRegisterResult.ContentType);
						break;
					case "/sessions":
						_logger.Debug("Routing POST /sessions");
						var userLoginResult = _userManager.LoginUser(handler, _dbHandler);
						handler.Reply(userLoginResult.Success ? 200 : 400, userLoginResult.Message, userLoginResult.ContentType);
						break;
					case "/packages":
						// create new package
						break;
					case "/transactions/packages":
						// create new transaction
						break;
					case "/battles":
						// create new battle
						break;
					case "/tradings":
						// create new trading
						break;
					default:
						handler.Reply(404);
						break;
				}
				// users
				// sessions
				// packages
				// transactions/packages
				// battles
				// tradings
				// tradings/{tradingdealid}
				break;
			case "PUT":
				// users/username
				// deck
				break;
			case "DELETE":
				// tradings/{tradingdealid}
				break;
		}
	}
}