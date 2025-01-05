using Microsoft.Extensions.DependencyInjection;
using MTCG.Server.Config;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util;
using System.Net;
using System.Net.Sockets;

namespace MTCG.Server.HTTP;

public class Server
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	private TcpListener _tcpListener;

	//private Router _router;

	private bool _running;

	private Uri _uri;

	public Server(string url)
	{
		var uri = new Uri(url);
		_uri = uri;
		_tcpListener = new TcpListener(IPAddress.Any, uri.Port);
		//_router = new Router();
		_running = true;
	}

	public void Start()
	{
		_tcpListener.Start();
		_logger.Info($"Server started on \"{_uri}\"");

		var serviceCollection = new ServiceCollection();
		ConfigureServices(serviceCollection);

		var serviceProvider = serviceCollection.BuildServiceProvider();
		var router = serviceProvider.GetRequiredService<Router>();

		while (_running)
		{
			// TODO: add tasking/asynchronous handling

			_logger.Info("Waiting for a connection..");
			var client = _tcpListener.AcceptTcpClient();
			Task.Run(() =>
			{
				using (var scope = serviceProvider.CreateScope())
				{
					IHandler handler = scope.ServiceProvider.GetRequiredService<IHandler>();
					handler.Handle(client);
					_logger.Debug($"Recieved new request: \"{handler.PlainMessage}\"");
					router.HandleIncoming(handler);
				}
			});

		}
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<DatabaseConnection>(provider =>
			new DatabaseConnection(
				$"Host={DatabaseCredentials.DB_HOST};Port={DatabaseCredentials.DB_PORT};Username={DatabaseCredentials.DB_USER};Password={DatabaseCredentials.DB_PASSWORD};Database={DatabaseCredentials.DB_NAME};Pooling=True"));


		services.AddScoped<ICardRepository, CardRepository>();
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddScoped<IPackageRepository, PackageRepository>();
		services.AddScoped<ITradeRepository, TradeRepository>();
		services.AddScoped<IDeckRepository, DeckRepository>();
		services.AddScoped<ITransactionRepository, TransactionRepository>();

		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IBattleService, BattleService>();
		services.AddScoped<ICardService, CardService>();
		services.AddScoped<IDeckService, DeckService>();
		services.AddScoped<ITradeService, TradeService>();
		services.AddScoped<ITransactionService, TransactionService>();

		services.AddScoped<IHelperService, HelperService>();
		services.AddScoped<IHandler, Handler>();
		services.AddScoped<Router>();

	}
}