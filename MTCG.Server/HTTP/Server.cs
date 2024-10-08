using System.Net;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public class Server
{
	private TcpListener _tcpListener;

	private Router _router;

	private bool _running;

	public Server(string uri)
	{
		_tcpListener = new TcpListener(IPAddress.Any, 8888);
		_router = new Router();
		_running = true;
	}

	public void Start()
	{
		_tcpListener.Start();

		while (_running)
		{
			Console.WriteLine("Waiting for a connection...");
			var client = _tcpListener.AcceptTcpClient();
			var handler = new Handler();
			handler.Handle(client);
			_router.HandleIncoming(handler);
		}
	}
}