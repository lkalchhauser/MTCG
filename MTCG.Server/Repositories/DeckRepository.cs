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
}