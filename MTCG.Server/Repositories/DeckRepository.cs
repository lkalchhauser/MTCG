using System.Data;
using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class DeckRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public int GetDeckIdFromUserId(int userId)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			SELECT id
			FROM deck
			WHERE user_id = @user_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		using IDataReader reader = dbCommand.ExecuteReader();
		return reader.Read() ? reader.GetInt32(0) : 0;
	}

	public List<int> GetAllCardIdsFromDeckId(int deckId)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
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

	public bool DeleteDeckById(int deckId)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			DELETE FROM deck
			WHERE id = @deck_id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@deck_id", DbType.Int32, deckId);
		return dbCommand.ExecuteNonQuery() == 1;
	}

	public int AddNewDeckToUserId(int userId)
	{
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO deck (user_id)
			VALUES (@user_id)
			RETURNING id
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@user_id", DbType.Int32, userId);
		return (int)(dbCommand.ExecuteScalar() ?? 0);
	}

	public bool AddCardToDeck(int deckId, int cardId)
	{
		_logger.Debug($"Adding card \"{cardId}\" to deck \"{deckId}\"");
		using IDbCommand dbCommand = _dbConn.CreateCommand("""
			INSERT INTO deck_card (deck_id, card_id)
			VALUES (@deck_id, @card_id)
			""");
		DatabaseConnection.AddParameterWithValue(dbCommand, "@deck_id", DbType.Int32, deckId);
		DatabaseConnection.AddParameterWithValue(dbCommand, "@card_id", DbType.Int32, cardId);
		return dbCommand.ExecuteNonQuery() == 1;
	}
}