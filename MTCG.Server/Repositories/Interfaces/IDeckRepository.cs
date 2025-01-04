namespace MTCG.Server.Repositories.Interfaces;

public interface IDeckRepository
{
	public int GetDeckIdFromUserId(int userId);
	public List<int> GetAllCardIdsFromDeckId(int deckId);
	public bool DeleteDeckById(int deckId);
	public int AddNewDeckToUserId(int userId);
	public bool AddCardToDeck(int deckId, int cardId);
}