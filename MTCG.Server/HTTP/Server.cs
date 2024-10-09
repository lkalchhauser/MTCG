using System.Net;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public class Server
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	private TcpListener _tcpListener;

	private Router _router;

	private bool _running;

	private Uri _uri;

	public Server(string url)
	{
		var uri = new Uri(url);
		_uri = uri;
		_tcpListener = new TcpListener(IPAddress.Any, uri.Port);
		_router = new Router();
		_running = true;
	}

	public void Start()
	{
		_tcpListener.Start();
		_logger.Info($"Server started on \"{_uri}\"");

		while (_running)
		{
			// TODO: add tasking/asynchronous handling
			_logger.Info("Waiting for a connection..");
			var client = _tcpListener.AcceptTcpClient();
			var handler = new Handler();
			handler.Handle(client);
			_logger.Debug($"Recieved new request: \"{handler.PlainMessage}\"");
			_router.HandleIncoming(handler);
		}
	}
}