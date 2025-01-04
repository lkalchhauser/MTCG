using MTCG.Server.Config;
using Npgsql;

namespace MTCG.Server.Util.DbUtil;

public class DbUtil
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	public void SetupDatabase()
	{
		_logger.Debug("Setting up Database...");
		var connection = new NpgsqlConnection(new DatabaseCredentials().GetConnectionString());
		connection.Open();

		// User table
		var dropUserTable = new NpgsqlCommand("DROP TABLE IF EXISTS users CASCADE", connection);
		dropUserTable.ExecuteNonQuery();

		var createUserTable = new NpgsqlCommand("""
		                                        CREATE TABLE users(
		                                        id SERIAL PRIMARY KEY,
		                                        username VARCHAR UNIQUE NOT NULL,
		                                        password VARCHAR NOT NULL,
		                                        token VARCHAR UNIQUE,
		                                        coins int DEFAULT 20
		                                        );
		                                        """, connection);
		createUserTable.ExecuteNonQuery();

		var dropUserInfoTable = new NpgsqlCommand("DROP TABLE IF EXISTS userinfo CASCADE", connection);
		dropUserInfoTable.ExecuteNonQuery();

		var createUserInfoTable = new NpgsqlCommand("""
		                                        CREATE TABLE userinfo(
		                                        user_id int UNIQUE PRIMARY KEY NOT NULL REFERENCES users(id) ON DELETE CASCADE,
		                                        name VARCHAR,
		                                        bio VARCHAR,
		                                        image VARCHAR
		                                        );
		                                        """, connection);
		createUserInfoTable.ExecuteNonQuery();

		// User stats
		var dropStatsTable = new NpgsqlCommand("DROP TABLE IF EXISTS stats CASCADE", connection);
		dropStatsTable.ExecuteNonQuery();
		var createStatsTable = new NpgsqlCommand("""
		                                        CREATE TABLE stats(
		                                        user_id int UNIQUE PRIMARY KEY NOT NULL REFERENCES users(id) ON DELETE CASCADE,
		                                        elo int DEFAULT 100,
		                                        wins int DEFAULT 0,
		                                        losses int DEFAULT 0,
		                                        draws int DEFAULT 0
		                                        );
		                                        """, connection);
		createStatsTable.ExecuteNonQuery();

		// Package table
		var dropPackageTable = new NpgsqlCommand("DROP TABLE IF EXISTS packages CASCADE", connection);
		dropPackageTable.ExecuteNonQuery();
		var createPackageTable = new NpgsqlCommand("""
		                                          CREATE TABLE packages(
		                                          id SERIAL PRIMARY KEY,
		                                          name VARCHAR UNIQUE NOT NULL,
		                                          rarity VARCHAR,
		                                          cost DECIMAL DEFAULT 5,
		                                          available_amount int DEFAULT 1
		                                          );
		                                          """, connection);
		createPackageTable.ExecuteNonQuery();

		// Card table
		var dropCardTable = new NpgsqlCommand("DROP TABLE IF EXISTS cards CASCADE", connection);
		dropCardTable.ExecuteNonQuery();
		var createCardTable = new NpgsqlCommand("""
		                                       CREATE TABLE cards(
		                                       id SERIAL PRIMARY KEY,
		                                       uuid VARCHAR NOT NULL,
		                                       name VARCHAR,
		                                       description VARCHAR,
		                                       damage int NOT NULL,
		                                       element VARCHAR NOT NULL,
		                                       type VARCHAR NOT NULL,
		                                       rarity VARCHAR,
		                                       race VARCHAR
		                                       );
		                                       """, connection);
		createCardTable.ExecuteNonQuery();

		// Package-Card relationship
		var dropPackageCardTable = new NpgsqlCommand("DROP TABLE IF EXISTS package_card CASCADE", connection);
		dropPackageCardTable.ExecuteNonQuery();
		var createPackageCardTable = new NpgsqlCommand("""
		                                               CREATE TABLE package_card(
		                                               package_id int NOT NULL REFERENCES packages(id) ON DELETE CASCADE,
		                                               card_id int NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
		                                               PRIMARY KEY (package_id, card_id)
		                                               );
		                                               """, connection);
		createPackageCardTable.ExecuteNonQuery();

		// User-Card relationship
		var dropUserCardTable = new NpgsqlCommand("DROP TABLE IF EXISTS user_card CASCADE", connection);
		dropUserCardTable.ExecuteNonQuery();
		var createUserCardTable = new NpgsqlCommand("""
		                                            CREATE TABLE user_card(
		                                            user_id int NOT NULL REFERENCES users(id) ON DELETE CASCADE,
		                                            card_id int NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
		                                            PRIMARY KEY (user_id, card_id),
		                                            quantity int DEFAULT 1,
		                                            locked_amount int DEFAULT 0
		                                            );
		                                            """, connection);
		createUserCardTable.ExecuteNonQuery();

		// Deck table
		var dropDeckTable = new NpgsqlCommand("DROP TABLE IF EXISTS deck CASCADE", connection);
		dropDeckTable.ExecuteNonQuery();
		var createDeckTable = new NpgsqlCommand("""
		                                        CREATE TABLE deck(
		                                        id SERIAL PRIMARY KEY,
		                                        user_id int UNIQUE REFERENCES users(id) ON DELETE CASCADE
		                                        );
		                                        """, connection);
		createDeckTable.ExecuteNonQuery();

		// Deck-Card relationship
		var dropDeckCardTable = new NpgsqlCommand("DROP TABLE IF EXISTS deck_card CASCADE", connection);
		dropDeckCardTable.ExecuteNonQuery();
		var createDeckCardTable = new NpgsqlCommand("""
		                                            CREATE TABLE deck_card(
		                                            deck_id int NOT NULL REFERENCES deck(id) ON DELETE CASCADE,
		                                            card_id int NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
		                                            PRIMARY KEY (deck_id, card_id)
		                                            );
		                                            """, connection);
		createDeckCardTable.ExecuteNonQuery();

		// Battle table
		var dropBattleTable = new NpgsqlCommand("DROP TABLE IF EXISTS battles CASCADE", connection);
		dropBattleTable.ExecuteNonQuery();
		var createBattleTable = new NpgsqlCommand("""
		                                          CREATE TABLE battles(
		                                          id SERIAL PRIMARY KEY,
		                                          player1_id int NOT NULL REFERENCES users(id) ON DELETE CASCADE,
		                                          player2_id int NOT NULL REFERENCES users(id) ON DELETE CASCADE,
		                                          result VARCHAR,
		                                          timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
		                                          );
		                                          """, connection);
		createBattleTable.ExecuteNonQuery();

		// Trade table
		var dropTradeTable = new NpgsqlCommand("DROP TABLE IF EXISTS trade_offers CASCADE", connection);
		dropTradeTable.ExecuteNonQuery();
		// TODO: maybe add a way to log when the trade is completed
		var createTradeTable = new NpgsqlCommand("""
		                                         CREATE TABLE trade_offers(
		                                         id SERIAL PRIMARY KEY,
		                                         offering_user_id int REFERENCES users(id) ON DELETE CASCADE,
		                                         offered_card_id int NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
		                                         desired_card_type VARCHAR,
		                                         desired_card_rarity VARCHAR,
		                                         desired_card_race VARCHAR,
		                                         desired_card_element VARCHAR,
		                                         desired_card_minimum_damage INT,
		                                         timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
		                                         status VARCHAR DEFAULT 'ACTIVE'
		                                         );
		                                         """, connection);
		createTradeTable.ExecuteNonQuery();

		// Trade accept table
		var dropTradeAcceptTable = new NpgsqlCommand("DROP TABLE IF EXISTS trade_accept CASCADE", connection);
		dropTradeAcceptTable.ExecuteNonQuery();
		var createTradeAcceptTable = new NpgsqlCommand("""
		                                               CREATE TABLE trade_accept(
		                                               trade_id int NOT NULL REFERENCES trade_offers(id) ON DELETE CASCADE,
		                                               accepted_user_id int NOT NULL REFERENCES users(id) ON DELETE CASCADE,
		                                               provided_card_id int NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
		                                               timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
		                                               PRIMARY KEY (trade_id, accepted_user_id)
		                                               );
		                                               """, connection);
		createTradeAcceptTable.ExecuteNonQuery();



		connection.Close();
	}
}