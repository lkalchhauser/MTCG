using MTCG.Server.Models;

namespace MTCG.Server.Repositories.Interfaces;

public interface ICardRepository
{
	public int AddCard(Card card);
	public Card? GetCardById(int id);
	public Card? GetCardByUuid(string uuid);
	public bool AddNewCardToUserStack(int userId, int cardId);
	public UserCardRelation? GetUserCardRelation(int userId, int cardId);
	public List<UserCardRelation> GetAllCardRelationsForUserId(int userId);
	public bool UpdateUserCardRelation(UserCardRelation userCardRelation);
	public bool RemoveCardUserStack(UserCardRelation userCardRelation);
}