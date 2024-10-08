using System.Net;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public class Server
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	private TcpListener _tcpListener;

	private Router _router;

	private bool _running;

	public Server(string uri)
	{
		var url = new Uri(uri);
		_tcpListener = new TcpListener(IPAddress.Any, url.Port);
		_router = new Router();
		_running = true;
	}

	public void Start()
	{
		_tcpListener.Start();

		while (_running)
		{
			_logger.Info("Waiting for a connection..");
			var client = _tcpListener.AcceptTcpClient();
			var handler = new Handler();
			handler.Handle(client);
			_logger.Debug($"Recieved new request: \"{handler.PlainMessage}\"");
			_router.HandleIncoming(handler);
		}
	}
}