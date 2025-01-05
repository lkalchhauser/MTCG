using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;
using System.Data;
using System.Text.Json;

namespace MTCG.Server.Repositories;

/*
 *	Repository for handling all database operations related to users.
 */
public class UserRepository(DatabaseConnection dbConn) : IUserRepository
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 * Gets a user by its username.
	 *	<param name="username">The username of the user</param>
	 *	<returns>The user with the given username</returns>
	 */
	public UserCredentials? GetUserByUsername(string username)
	{
		_logger.Debug($"Trying to get user \"{username}\" from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Gets a user by its id.
	 *	<param name="id">The id of the user</param>
	 *	<returns>The user with the given id</returns>
	 */
	public UserCredentials? GetUserById(int id)
	{
		_logger.Debug($"Trying to get user \"{id}\" from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT *
			FROM users
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, id);

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

	/**
	 * Gets a user by its token.
	 *	<param name="token">The token of the user</param>
	 *	<returns>The user with the given token</returns>
	 */
	public UserCredentials? GetUserByToken(string token)
	{
		_logger.Debug($"Trying to get user from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Adds a user to the database.
	 *	<param name="user">The user to add</param>
	 *	<returns>The id of the added user</returns>
	 */
	public int AddUser(UserCredentials user)
	{
		_logger.Debug($"Adding user \"{JsonSerializer.Serialize(user)}\" to db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO users (username, password)
			VALUES (@username, @password)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@username", DbType.String, user.Username);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@password", DbType.String, user.Password);
		var userId = (int)(dbCommand.ExecuteScalar() ?? 0);
		return userId;
	}

	/**
	 * Updates a user in the database.
	 *	<param name="user">The user to update</param>
	 *	<returns>True if the user was updated, false otherwise</returns>
	 */
	public bool UpdateUser(UserCredentials user)
	{
		_logger.Debug($"Updating user \"{JsonSerializer.Serialize(user)}\" in db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Removes a user from the database.
	 *	<param name="userId">The id of the user to remove</param>
	 *	<returns>True if the user was removed, false otherwise</returns>
	 */
	public bool RemoveUser(int userId)
	{
		_logger.Debug($"Removing user with id {userId} from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			DELETE FROM users
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, userId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Gets all users from the database.
	 *	<returns>A list of all users</returns>
	 */
	public UserInfo? GetUserInfoByUser(UserCredentials user)
	{
		_logger.Debug($"Trying to get userinfo from \"{user.Username}\" from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Adds userinfo to the database.
	 *	<param name="userInfo">The userinfo to add</param>
	 *	<returns>True if the userinfo was added, false otherwise</returns>
	 */
	public bool AddUserInfo(UserInfo userInfo)
	{
		_logger.Debug($"Adding userinfo \"{JsonSerializer.Serialize(userInfo)}\" to db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO userinfo (user_id, name, bio, image)
			VALUES (@user_id, @name, @bio, @image)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userInfo.Id);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, userInfo.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@bio", DbType.String, userInfo.Bio);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@image", DbType.String, userInfo.Image);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Updates userinfo in the database.
	 *	<param name="userInfo">The userinfo to update</param>
	 *	<returns>True if the userinfo was updated, false otherwise</returns>
	 */
	public bool UpdateUserInfo(UserInfo userInfo)
	{
		_logger.Debug($"Updating userinfo \"{JsonSerializer.Serialize(userInfo)}\" in db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Removes userinfo from the database.
	 *	<param name="userId">The id of the user whose userinfo to remove</param>
	 *	<returns>True if the userinfo was removed, false otherwise</returns>
	 */
	public bool RemoveUserInfoByUserId(int userId)
	{
		_logger.Debug($"Removing userinfo with user_id {userId} from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			DELETE FROM userinfo
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Gets all user stats from the database.
	 *	<returns>A list of all user stats</returns>
	 */
	public UserStats? GetUserStats(IHandler handler)
	{
		_logger.Debug($"Trying to get stats from \"{handler.AuthorizedUser.Username}\" from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Adds user stats to the database.
	 *	<param name="userStats">The user stats to add</param>
	 *	<returns>True if the user stats were added, false otherwise</returns>
	 */
	public bool AddUserStats(UserStats userStats)
	{
		_logger.Debug($"Adding stats \"{JsonSerializer.Serialize(userStats)}\" to db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Updates user stats in the database.
	 *	<param name="userStats">The user stats to update</param>
	 *	<returns>True if the user stats were updated, false otherwise</returns>
	 */
	public bool UpdateUserStats(UserStats userStats)
	{
		_logger.Debug($"Updating userstats \"{JsonSerializer.Serialize(userStats)}\" in db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			UPDATE stats
			SET elo = @elo, wins = @wins, losses = @losses, draws = @draws
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@elo", DbType.Int32, userStats.Elo);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@wins", DbType.Int32, userStats.Wins);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@losses", DbType.Int32, userStats.Losses);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@draws", DbType.Int32, userStats.Draws);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userStats.Id);

		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 *	Get all stats from the database.
	 *	<returns>A list of UserStats containing all entries</returns>
	 */
	public List<UserStats> GetAllStats()
	{
		_logger.Debug($"Trying to get all stats from db");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
		                                                   SELECT *
		                                                   FROM stats
		                                                   """);

		using IDataReader reader = dbCommand.ExecuteReader();
		List<UserStats> stats = [];
		while (reader.Read())
		{
			stats.Add(new UserStats()
			{
				Id = reader.GetInt32(0),
				Elo = reader.GetInt32(1),
				Wins = reader.GetInt32(2),
				Losses = reader.GetInt32(3),
				Draws = reader.GetInt32(4),
			});
		}
		return stats;
	}
}