using System.Data;
using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class UserRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public UserCredentials? GetUserByUsername(string username)
	{
		_logger.Debug($"Trying to get user \"{username}\" from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM users
			WHERE username = @username
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, username);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserCredentials()
			{
				Id = reader.GetInt32(0),
				Username = reader.GetString(1),
				Password = reader.GetString(2),
				Token = reader[3] as string ?? null,
				Coins = reader.GetInt32(4)
			};
		}
		return null;
	}

	public UserCredentials? GetUserByToken(string token)
	{
		_logger.Debug($"Trying to get user from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
		                                                   SELECT *
		                                                   FROM users
		                                                   WHERE token = @token
		                                                   """);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@token", DbType.String, token);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserCredentials()
			{
				Id = reader.GetInt32(0),
				Username = reader.GetString(1),
				Password = reader.GetString(2),
				Token = reader[3] as string ?? null,
				Coins = reader.GetInt32(4)
			};
		}
		return null;
	}

	public int AddUser(UserCredentials user)
	{
		_logger.Debug($"Adding user \"{JsonSerializer.Serialize(user)}\" to db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO users (username, password)
			VALUES (@username, @password)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, user.Username);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@password", DbType.String, user.Password);
		var userId = (int)(dbCommand.ExecuteScalar() ?? 0);
		return userId;
	}

	public bool UpdateUser(UserCredentials user)
	{
		_logger.Debug($"Updating user \"{JsonSerializer.Serialize(user)}\" in db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			UPDATE users
			SET username = @username, password = @password, token = @token, coins = @coins
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, user.Username);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@password", DbType.String, user.Password);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@token", DbType.String, user.Token);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, user.Id);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@coins", DbType.Int32, user.Coins);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool RemoveUser(int userId)
	{
		_logger.Debug($"Removing user with id {userId} from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			DELETE FROM users
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, userId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public UserInfo? GetUserInfoByUser(UserCredentials user)
	{
		_logger.Debug($"Trying to get userinfo from \"{user.Username}\" from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM userinfo
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, user.Id);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserInfo()
			{
				Id = reader.GetInt32(0),
				Name = reader.GetString(1),
				Bio = reader.GetString(2),
				Image = reader.GetString(3),
			};
		}
		return null;
	}

	public bool AddUserInfo(UserInfo userInfo)
	{
		_logger.Debug($"Adding userinfo \"{JsonSerializer.Serialize(userInfo)}\" to db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO userinfo (user_id, name, bio, image)
			VALUES (@user_id, @name, @bio, @image)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userInfo.Id);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, userInfo.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@bio", DbType.String, userInfo.Bio);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@image", DbType.String, userInfo.Image);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool UpdateUserInfo(UserInfo userInfo)
	{
		_logger.Debug($"Updating userinfo \"{JsonSerializer.Serialize(userInfo)}\" in db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			UPDATE userinfo
			SET name = @name, bio = @bio, image = @image
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, userInfo.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@bio", DbType.String, userInfo.Bio);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@image", DbType.String, userInfo.Image);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userInfo.Id);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool RemoveUserInfoByUserId(int userId)
	{
		_logger.Debug($"Removing userinfo with user_id {userId} from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			DELETE FROM userinfo
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public UserStats? GetUserStats(Handler handler)
	{
		_logger.Debug($"Trying to get stats from \"{handler.AuthorizedUser.Username}\" from db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM stats
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, handler.AuthorizedUser.Id);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserStats()
			{
				Id = reader.GetInt32(0),
				Elo = reader.GetInt32(1),
				Wins = reader.GetInt32(2),
				Losses = reader.GetInt32(3),
				Draws = reader.GetInt32(4),
			};
		}
		return null;
	}

	public bool AddUserStats(UserStats userStats)
	{
		_logger.Debug($"Adding stats \"{JsonSerializer.Serialize(userStats)}\" to db");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO stats (user_id, elo, wins, losses, draws)
			VALUES (@user_id, @elo, @wins, @losses, @draws)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userStats.Id);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@elo", DbType.Int32, userStats.Elo);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@wins", DbType.Int32, userStats.Wins);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@losses", DbType.Int32, userStats.Losses);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@draws", DbType.Int32, userStats.Draws);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}