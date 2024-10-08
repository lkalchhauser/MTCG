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
			_connection = new NpgsqlConnection(new DatabaseCredentials().GetConnectionString());
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

	public bool RegisterUser(UserCredentials credentials)
	{
		var command = new NpgsqlCommand($"insert into users (username, password) values ('{credentials.Username}', '{credentials.Password}')", _connection);
		command.ExecuteNonQuery();
		return true;
	}

	public bool DoesUserExist(string username)
	{
		var command = new NpgsqlCommand($"select * from users where username = '{username}'", _connection);
		var reader = command.ExecuteReader();
		var hasRows = reader.HasRows;
		reader.Close();
		return hasRows;
	}

	public string LoginUser(UserCredentials credentials)
	{
		// TODO: maybe change this to use npgsql parameters
		var command = new NpgsqlCommand($"select password from users where username = '{credentials.Username}'", _connection);
		var reader = command.ExecuteReader();

		if (!reader.HasRows)
		{
			reader.Close();
			return string.Empty;
		}

		reader.Read();
		var pwHash = (string)reader[0];
		reader.Close();

		var passwordIsValid = Helper.VerifyPassword(credentials.Password, pwHash);

		return !passwordIsValid ? string.Empty : Helper.GenerateToken(credentials.Username);
	}
}