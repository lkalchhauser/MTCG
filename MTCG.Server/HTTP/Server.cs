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
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	private TcpListener _tcpListener;

	private bool _running;

	private Uri _uri;

	public Server(string url)
	{
		var uri = new Uri(url);
		_uri = uri;
		_tcpListener = new TcpListener(IPAddress.Any, uri.Port);
		_running = true;
	}

	/**
	 *	Starts the server and listens for incoming connections
	 */
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
			_logger.Info("Waiting for a connection..");
			var client = _tcpListener.AcceptTcpClient();

			// new task for each request
			Task.Run(() =>
			{
				using var scope = serviceProvider.CreateScope();
				IHandler handler = scope.ServiceProvider.GetRequiredService<IHandler>();
				handler.Handle(client);
				_logger.Debug($"Recieved new request: \"{handler.PlainMessage}\"");
				router.HandleIncoming(handler);
			});

		}
	}

	/**
	 *	Configures all the services and repositories for usage with DI
	 *	<param name="services">The service collection to use</param>
	 */
	public void ConfigureServices(IServiceCollection services)
	{
		_logger.Debug("Configuring Services...");
		services.AddScoped<DatabaseConnection>(provider =>
			new DatabaseConnection(
				$"Host={DatabaseCredentials.DB_HOST};Port={DatabaseCredentials.DB_PORT};Username={DatabaseCredentials.DB_USER};Password={DatabaseCredentials.DB_PASSWORD};Database={DatabaseCredentials.DB_NAME};Pooling=True"));

		_logger.Debug("Configuring Repositories...");
		services.AddScoped<ICardRepository, CardRepository>();
		_logger.Debug("Added CardRepository");
		services.AddScoped<IUserRepository, UserRepository>();
		_logger.Debug("Added UserRepository");
		services.AddScoped<IPackageRepository, PackageRepository>();
		_logger.Debug("Added PackageRepository");
		services.AddScoped<ITradeRepository, TradeRepository>();
		_logger.Debug("Added TradeRepository");
		services.AddScoped<IDeckRepository, DeckRepository>();
		_logger.Debug("Added DeckRepository");
		services.AddScoped<ITransactionRepository, TransactionRepository>();
		_logger.Debug("Added TransactionRepository");

		_logger.Debug("Configuring Services...");
		services.AddScoped<IUserService, UserService>();
		_logger.Debug("Added UserService");
		services.AddScoped<IBattleService, BattleService>();
		_logger.Debug("Added BattleService");
		services.AddScoped<ICardService, CardService>();
		_logger.Debug("Added CardService");
		services.AddScoped<IDeckService, DeckService>();
		_logger.Debug("Added DeckService");
		services.AddScoped<ITradeService, TradeService>();
		_logger.Debug("Added TradeService");
		services.AddScoped<ITransactionService, TransactionService>();
		_logger.Debug("Added TransactionService");
		services.AddScoped<IHelperService, HelperService>();
		_logger.Debug("Added HelperService");

		_logger.Debug("Configuring Handler and Router...");
		services.AddScoped<IHandler, Handler>();
		services.AddScoped<Router>();

	}
}