using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface IDeckService
{
	public Result GetDeckForCurrentUser(IHandler handler, bool forceJsonFormat = false);
	public Result SetDeckForCurrentUser(IHandler handler);
	public void RemoveAndUnlockDeck(int deckId, UserCredentials user, List<Card> currentDeck);
	public void DeleteDeckAndCardsFromUser(int deckId, UserCredentials user, List<Card> currentDeck);
	public bool IsCardAvailableForUser(Card card, UserCredentials user, List<Card> currentDeck);
}