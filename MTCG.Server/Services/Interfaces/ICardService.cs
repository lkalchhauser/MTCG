using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface ICardService
{
	public Result CreatePackageAndCards(IHandler handler);
	public int AddCardIfNotExists(Card card);
	public bool AddCardsToUserStack(int userId, List<Card> cards);
	public bool AddCardToUserStack(int userId, int cardId);
	public bool RemoveCardsFromUserStack(int userId, List<Card> cards);
	public bool RemoveCardFromUserStack(int userId, int cardId);
	public bool LockCardInUserStack(int userId, int cardId);
	public bool UnlockCardInUserStack(int userId, int cardId);
	public Result ShowAllCardsForUser(IHandler handler);
}