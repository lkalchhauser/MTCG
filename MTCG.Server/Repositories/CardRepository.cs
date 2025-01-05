using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;
using System.Data;

namespace MTCG.Server.Repositories;

/**
 * This class is responsible for handling all database operations related to cards
 *	<param name="dbConn">The database connection</param>
 */
public class CardRepository(DatabaseConnection dbConn) : ICardRepository
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 * Adds a card to the database
	 *	<param name="card">The card to add</param>
	 *	<returns>The id of the added card</returns>
	 */
	public int AddCard(Card card)
	{
		_logger.Debug($"Adding card \"{card.Name}\" to the DB");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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
		return card.Id;
	}

	/**
	 * Gets a card by its id
	 *	<param name="id">The id of the card</param>
	 *	<returns>The card with the given id</returns>
	 */
	public Card? GetCardById(int id)
	{
		_logger.Debug($"Getting card with \"{id}\" from the DB");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Gets a card by its uuid
	 *	<param name="uuid">The uuid of the card</param>
	 *	<returns>The card with the given uuid</returns>
	 */
	public Card? GetCardByUuid(string uuid)
	{
		_logger.Debug($"Getting card with uuid \"{uuid}\" from the DB");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT *
			FROM cards
			WHERE uuid = @uuid
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@uuid", DbType.String, uuid);
		using IDataReader reader = dbCommand.ExecuteReader();
		if (!reader.Read()) return null;

		var card = new Card()
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

	/**
	 *	Adds a card to a user's stack
	 *	<param name="userId">the id of the user to add the card to</param>
	 *	<param name="cardId">the id of the card to add to the stack</param>
	 *	<returns>true if card was successfully added</returns>
	 */
	public bool AddNewCardToUserStack(int userId, int cardId)
	{
		_logger.Debug($"Adding 1 card \"{cardId}\" to user \"{userId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO user_card (user_id, card_id)
			VALUES (@user_id, @card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Gets the user-card relation
	 *	<param name="userId">The id of the user</param>
	 * <param name="cardId">The id of the card</param>
	 *	<returns>The UserCardRelation of the given user & card</returns>
	 */
	public UserCardRelation? GetUserCardRelation(int userId, int cardId)
	{
		_logger.Debug($"Getting user card relation for card \"{cardId}\" and user \"{userId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Gets all card relations for a user
	 *	<param name="userId">The id of the user</param>
	 *	<returns>A list of all card relations for the given user</returns>
	 */
	public List<UserCardRelation> GetAllCardRelationsForUserId(int userId)
	{
		_logger.Debug($"Getting all card relations for user \"{userId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT *
			FROM user_card
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		using IDataReader reader = dbCommand.ExecuteReader();
		List<UserCardRelation> cardIds = [];
		while (reader.Read())
		{
			cardIds.Add(new UserCardRelation()
			{
				UserId = reader.GetInt32(0),
				CardId = reader.GetInt32(1),
				Quantity = reader.GetInt32(2),
				LockedAmount = reader.GetInt32(3)
			});
		}
		return cardIds;
	}

	/**
	 * Updates the user-card relation
	 *	<param name="userCardRelation">The user-card relation to update</param>
	 *	<returns>true if the relation was successfully updated</returns>
	 */
	public bool UpdateUserCardRelation(UserCardRelation userCardRelation)
	{
		_logger.Debug($"Updating user card relation for card \"{userCardRelation.CardId}\" and user \"{userCardRelation.UserId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
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

	/**
	 * Removes a card from a user's stack
	 *	<param name="userCardRelation">The user-card relation to remove</param>
	 *	<returns>true if the card was successfully removed</returns>
	 */
	public bool RemoveCardUserStack(UserCardRelation userCardRelation)
	{
		_logger.Debug($"Removing card \"{userCardRelation.CardId}\" from user \"{userCardRelation.UserId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			DELETE FROM user_card
			WHERE user_id = @user_id
			AND card_id = @card_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userCardRelation.UserId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, userCardRelation.CardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}