using System.Data;
using MTCG.Server.Models;
using MTCG.Server.Services;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Repositories;

public class CardRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public bool AddCard(Card card)
	{
		_logger.Debug($"Adding card \"{card.Name}\" to the DB");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO cards (uuid, name, description, damage, element, type, rarity, race)
			VALUES (@uuid, @name, @description, @damage, @element, @type, @rarity, @race)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@uuid", DbType.String, card.UUID);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@name", DbType.String, card.Name);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@description", DbType.String, card.Description);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@damage", DbType.Int32, card.Damage);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@element", DbType.String, card.Element.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@type", DbType.String, card.Type.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@rarity", DbType.String, card.Rarity.ToString());
		DatabaseConnection.AddParameterWithValue(dbCommand, "@race", DbType.String, card.Race.ToString());
		card.Id = (int)(dbCommand.ExecuteScalar() ?? 0);
		return card.Id != 0;
	}

	public Card? GetCardById(int id)
	{
		_logger.Debug($"Getting card with \"{id}\" from the DB");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM cards
			WHERE id = @id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@id", DbType.Int32, id);
		using IDataReader reader = dbCommand.ExecuteReader();
		if (!reader.Read()) return null;

		Card card;
		card = new Card()
		{
			Id = reader.GetInt32(0),
			UUID = reader.GetString(1),
			Name = reader.GetString(2),
			Description = reader.GetString(3),
			Damage = reader.GetFloat(4),
			Element = Enum.Parse<Element>(reader.GetString(5)),
			Type = Enum.Parse<CardType>(reader.GetString(6)),
			Rarity = Enum.Parse<Rarity>(reader.GetString(7))
		};
		if (Enum.TryParse<Race>(reader.GetString(8), out var race))
		{
			card.Race = race;
		}
		return card;
	}

	public bool AddNewCardToUserStack(int userId, int cardId)
	{
		_logger.Debug($"Adding 1 card \"{cardId}\" to user \"{userId}\"");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO user_card (user_id, card_id)
			VALUES (@user_id, @card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public UserCardRelation? GetUserCardRelation(int userId, int cardId)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT *
			FROM user_card
			WHERE user_id = @user_id
			AND card_id = @card_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);

		using IDataReader reader = dbCommand.ExecuteReader();
		if (reader.Read())
		{
			return new UserCardRelation()
			{
				UserId = reader.GetInt32(0),
				CardId = reader.GetInt32(1),
				Quantity = reader.GetInt32(2),
				LockedAmount = reader.GetInt32(3)
			};
		}
		return null;
	}

	public bool UpdateUserStack(UserCardRelation userCardRelation)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			UPDATE user_card
			SET quantity = @quantity, locked_amount = @locked_amount
			WHERE user_id = @user_id
			AND card_id = @card_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userCardRelation.UserId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, userCardRelation.CardId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@quantity", DbType.Int32, userCardRelation.Quantity);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@locked_amount", DbType.Int32, userCardRelation.LockedAmount);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public bool RemoveCardUserStack(UserCardRelation userCardRelation)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			DELETE FROM user_card
			WHERE user_id = @user_id
			AND card_id = @card_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userCardRelation.UserId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, userCardRelation.CardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}