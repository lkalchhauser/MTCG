using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;
using System.Data;

namespace MTCG.Server.Repositories;

/**
 * Repository for handling all database operations related to decks.
 */
public class DeckRepository(DatabaseConnection dbConn) : IDeckRepository
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 * Gets the deck id for a given user id.
	 *	<param name="userId">The user id</param>
	 *	<returns>The deck id for the given user id</returns>
	 */
	public int GetDeckIdFromUserId(int userId)
	{
		_logger.Debug($"Getting deck id for user \"{userId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT id
			FROM deck
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		using IDataReader reader = dbCommand.ExecuteReader();
		return reader.Read() ? reader.GetInt32(0) : 0;
	}

	/**
	 * Gets all card ids from a given deck id.
	 *	<param name="deckId">The deck id</param>
	 *	<returns>A list of all card ids from the given deck id</returns>
	 */
	public List<int> GetAllCardIdsFromDeckId(int deckId)
	{
		_logger.Debug($"Getting all card ids from deck \"{deckId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			SELECT card_id
			FROM deck_card
			WHERE deck_id = @deck_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@deck_id", DbType.Int32, deckId);
		using IDataReader reader = dbCommand.ExecuteReader();
		List<int> cardIds = [];
		while (reader.Read())
		{
			cardIds.Add(reader.GetInt32(0));
		}

		return cardIds;
	}

	/**
	 * Deletes a deck by its id.
	 *	<param name="deckId">The id of the deck</param>
	 *	<returns>True if the deck was deleted, false otherwise</returns>
	 */
	public bool DeleteDeckById(int deckId)
	{
		_logger.Debug($"Deleting deck \"{deckId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			DELETE FROM deck
			WHERE id = @deck_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@deck_id", DbType.Int32, deckId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	/**
	 * Adds a new deck to a user.
	 *	<param name="userId">The id of the user</param>
	 *	<returns>The id of the new deck</returns>
	 */
	public int AddNewDeckToUserId(int userId)
	{
		_logger.Debug($"Adding new deck to user \"{userId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO deck (user_id)
			VALUES (@user_id)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		return (int)(dbCommand.ExecuteScalar() ?? 0);
	}

	/**
	 * Adds a card to a deck.
	 *	<param name="deckId">The id of the deck</param>
	 *	<param name="cardId">The id of the card</param>
	 *	<returns>True if the card was added to the deck, false otherwise</returns>
	 */
	public bool AddCardToDeck(int deckId, int cardId)
	{
		_logger.Debug($"Adding card \"{cardId}\" to deck \"{deckId}\"");
		using IDbCommand dbCommand = dbConn.CreateCommand("""
			INSERT INTO deck_card (deck_id, card_id)
			VALUES (@deck_id, @card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@deck_id", DbType.Int32, deckId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}