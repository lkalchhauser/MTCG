using System.Net;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public class Server
{
	private TcpListener _tcpListener;

	private Handler _handler;

	private Router _router;

	private bool _running;

	public Server(string uri)
	{
		_tcpListener = new TcpListener(IPAddress.Any, 8888);
		_handler = new Handler();
		_router = new Router();
		_running = true;
	}

	public void Start()
	{
		_tcpListener.Start();

		while (_running)
		{
			Console.WriteLine("Waiting for a connection...");
			TcpClient client = _tcpListener.AcceptTcpClient();

			Task.Run(() =>
			{
				_handler.Handle(client);
				_router.HandleIncoming(_handler);
			});
		}
	}
}