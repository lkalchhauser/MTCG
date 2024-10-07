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
	private readonly DatabaseHandler _dbHandler = DatabaseHandler.Instance;
	private UserManager _userManager = new UserManager();

	public void HandleIncoming(Handler handler)
	{
		Console.WriteLine(handler.Method);

		switch (handler.Method)
		{
			case "GET":
				switch(handler.Path)
				{
					case "/":
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
						var userRegister = _userManager.RegisterUser(handler);

						handler.Reply(userRegister.Success ? 200 : 400, userRegister.Message);
						break;
					case "/sessions":
						var userToken = _userManager.LoginUser(handler);
						handler.Reply(userToken == "" ? 400 : 200, userToken);
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

		handler.Reply(200);
	}
}