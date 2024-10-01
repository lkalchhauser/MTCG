using System.Net.Sockets;
using System.Reflection.Metadata;

namespace MTCG.Server.HTTP;

public class Router
{
	public void HandleIncoming(Handler handler)
	{
		Console.WriteLine(handler.Method);

		if (handler.Method == "GET")
		{
			// users/username
			// cards
			// deck
			// stats
			// scoreboard
			// tradings

		}
		else if (handler.Method == "POST")
		{
			// users
			// sessions
			// packages
			// transactions/packages
			// battles
			// tradings
			// tradings/{tradingdealid}

		}
		else if (handler.Method == "PUT")
		{
			// users/username
			// deck
		}
		else if (handler.Method == "DELETE")
		{
			// tradings/{tradingdealid}
		}

		handler.Reply(200);
	}
}