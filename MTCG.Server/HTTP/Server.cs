using System.Net;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public class Server
{
	private TcpListener _tcpListener;

	private Handler _handler;

	public Server(string uri)
	{
		_tcpListener = new TcpListener(IPAddress.Any, 8888);
		_handler = new Handler();
	}

	public void Start()
	{
		_tcpListener.Start();
		while (true)
		{
			Console.WriteLine("Waiting for a connection...");
			TcpClient client = _tcpListener.AcceptTcpClient();

			Task.Run(() =>
			{
				_handler.Handle(client);
				HandleIncoming();
			});
		}
	}

	public void HandleIncoming()
	{
		Console.WriteLine(_handler.Path);
		_handler.Reply(200);
	}
}