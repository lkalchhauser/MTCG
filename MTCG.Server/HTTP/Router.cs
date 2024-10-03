using System.Net.Sockets;
using System.Reflection.Metadata;

namespace MTCG.Server.HTTP;

public class Router
{
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