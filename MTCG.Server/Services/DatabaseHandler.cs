using MTCG.Server.Config;
using MTCG.Server.Models;
using MTCG.Server.Util;
using Npgsql;

namespace MTCG.Server.Services;

public class DatabaseHandler
{
	private static DatabaseHandler? _instance = null;

	private static readonly object _lock = new object();

	private NpgsqlConnection _connection;

	public static DatabaseHandler Instance
	{
		get
		{
			lock (_lock)
			{
				_instance ??= new DatabaseHandler();

				return _instance;
			}

		}
	}

	public bool SetupDbConnection()
	{
		try
		{
			_connection = new NpgsqlConnection(new Credentials().GetConnectionString());
			_connection.Open();
			Console.WriteLine("Database connection established!");
			return true;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return false;
		}
	}

	public bool CloseDbConnection()
	{
		_connection.Close();
		Console.WriteLine("Database connection closed!");
		return true;
	}

	public string RegisterUser(UserCredentials credentials)
	{
		return Helper.GenerateToken(credentials.Username);
	}
}