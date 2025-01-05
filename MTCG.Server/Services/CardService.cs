using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.HelperClasses;
using System.Text.Json;

namespace MTCG.Server.Services;

/**
 * Service for handling card related operations
 */
public class CardService(ICardRepository cardRepository, IPackageRepository packageRepository)
	: ICardService
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	/**
	 * Creates a new package and adds the cards to the database (if not exist).
	 *	<param name="handler">The handler containing the payload</param>
	 *	<returns>A result object containing information about the success of the operation</returns>
	 */
	public Result CreatePackageAndCards(IHandler handler)
	{
		_logger.Debug("CreatePackageAndCards - Starting");
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("CreatePackageAndCards - No valid payload data found");
			return new Result(false, "Badly formatted data sent!", statusCode: 400);
		}

		var package = JsonSerializer.Deserialize<Package>(handler.Payload);

		// check if a package with the same name already exists - currently package names have to be unique
		var packageByName = packageRepository.GetPackageIdByName(package.Name);

		if (packageByName != null)
		{
			_logger.Debug("CreatePackageAndCards - Package with this name already exists, increasing available amount");
			packageByName.AvailableAmount++;
			packageRepository.UpdatePackage(packageByName);
			return new Result(true, "Package with this name already exists, increased available amount!", statusCode: 200);
		}

		List<int> cardIds = []; // should always be 5 long
		foreach (var packageCard in package.Cards)
		{
			// if the card with this uuid already exists, we don't add a new one and return that one (cards can be in multiple packages)
			var addCard = AddCardIfNotExists(packageCard);
			if (addCard != 0)
			{
				cardIds.Add(addCard);
				continue;
			}

			_logger.Debug($"Failed to add card \"{packageCard.Name}\" to database");
			return new Result(false, "Failed to add card to database!", statusCode: 400);
		}

		packageRepository.AddPackage(package);

		foreach (var cardId in cardIds)
		{
			if (packageRepository.AddPackageCardRelation(package.Id, cardId)) continue;

			_logger.Debug($"Failed to add card \"{cardId}\" to package \"{package.Id}\"");
			return new Result(false, "Failed to add card to package", statusCode: 400);
		}

		_logger.Debug("CreatePackageAndCards - Package successfully added");
		return new Result(true, "Package successfully added!", statusCode: 201);
	}

	/**
	 * Adds a card to the database if it does not exist.
	 *	<param name="card">The card to add</param>
	 *	<returns>The id of the added card</returns>
	 */
	public int AddCardIfNotExists(Card card)
	{
		_logger.Debug($"AddCardIfNotExists - Starting for card \"{card.Name}\"");
		var cardFromDb = cardRepository.GetCardByUuid(card.UUID);
		return cardFromDb?.Id ?? cardRepository.AddCard(card);
	}

	/**
	 * Adds multiple cards to the user stack.
	 *	<param name="card">The cards to add</param>
	 *	<returns>True if the cards were added</returns>
	 */
	public bool AddCardsToUserStack(int userId, List<Card> cards)
	{
		_logger.Debug($"AddCardsToUserStack - Adding {cards.Count} cards to user {userId}");
		foreach (var card in cards)
		{
			AddCardToUserStack(userId, card.Id);
		}
		// TODO: not best return value
		return true;
	}

	/**
	 * Adds a card to the user stack.
	 *	<param name="cardId">The id of the card to add</param>
	 *	<returns>True if the card was added, otherwise false</returns>
	 */
	public bool AddCardToUserStack(int userId, int cardId)
	{
		_logger.Debug($"AddCardToUserStack - Adding card \"{cardId}\" to user \"{userId}\"");
		var relation = cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return cardRepository.AddNewCardToUserStack(userId, cardId);
		}

		relation.Quantity++;
		return cardRepository.UpdateUserCardRelation(relation);
	}

	/**
	 * Removes multiple cards from the user stack.
	 *	<param name="card">The cards to remove</param>
	 *	<returns>True if the cards were removed</returns>
	 */
	public bool RemoveCardsFromUserStack(int userId, List<Card> cards)
	{
		_logger.Debug($"RemoveCardsFromUserStack - Removing {cards.Count} cards from user {userId}");
		foreach (var card in cards)
		{
			RemoveCardFromUserStack(userId, card.Id);
		}
		// TODO: not best return value
		return true;
	}

	/**
	 * Removes a card from the user stack.
	 *	<param name="cardId">The id of the card to remove</param>
	 *	<returns>True if the card was removed, otherwise false</returns>
	 */
	public bool RemoveCardFromUserStack(int userId, int cardId)
	{
		_logger.Debug($"RemoveCardFromUserStack - Removing card \"{cardId}\" from user \"{userId}\"");
		var relation = cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return false;
		}

		if ((relation.Quantity - relation.LockedAmount) <= 0) return false;

		if (relation.Quantity == 1)
		{
			_logger.Debug($"RemoveCardFromUserStack - Removing last card \"{cardId}\" from user \"{userId}\" (deleting the relation)");
			cardRepository.RemoveCardUserStack(relation);
			return true;
		}

		_logger.Debug($"RemoveCardFromUserStack - Removing quantity of one card \"{cardId}\" from user \"{userId}\"");
		relation.Quantity--;
		cardRepository.UpdateUserCardRelation(relation);
		return true;
	}

	/**
	 * Locks a card in the user stack.
	 *	<param name="cardId">The id of the card to lock</param>
	 *	<returns>True if the card was locked, otherwise false</returns>
	 */
	public bool LockCardInUserStack(int userId, int cardId)
	{
		_logger.Debug($"LockCardInUserStack - Locking card \"{cardId}\" for user \"{userId}\"");
		var relation = cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return false;
		}

		if (relation.Quantity <= relation.LockedAmount) return false;

		_logger.Debug($"LockCardInUserStack - Increasing locked amount of card \"{cardId}\" for user \"{userId}\"");
		relation.LockedAmount++;
		cardRepository.UpdateUserCardRelation(relation);
		return true;
	}

	/**
	 * Unlocks a card in the user stack.
	 *	<param name="cardId">The id of the card to unlock</param>
	 *	<returns>True if the card was unlocked, otherwise false</returns>
	 */
	public bool UnlockCardInUserStack(int userId, int cardId)
	{
		_logger.Debug($"UnlockCardInUserStack - Unlocking card \"{cardId}\" for user \"{userId}\"");

		var relation = cardRepository.GetUserCardRelation(userId, cardId);

		if (relation is not { LockedAmount: > 0, Quantity: > 0 } || relation.Quantity < relation.LockedAmount)
			return false;

		_logger.Debug($"UnlockCardInUserStack - Decreasing locked amount of card \"{cardId}\" for user \"{userId}\"");
		relation.LockedAmount--;
		cardRepository.UpdateUserCardRelation(relation);
		return true;
	}

	/**
	 * Shows all cards for a user.
	 *	<param name="handler">The handler containing the user</param>
	 *	<returns>A result object containing the cards and info about the success</returns>
	 */
	public Result ShowAllCardsForUser(IHandler handler)
	{
		_logger.Debug("ShowAllCardsForUser - Starting");
		var userCardRelations = cardRepository.GetAllCardRelationsForUserId(handler.AuthorizedUser.Id);
		if (userCardRelations.Count == 0)
		{
			_logger.Debug($"No cards found for user {handler.AuthorizedUser.Username}");
			return new Result(true, "", statusCode: 204);
		}
		List<UserCardsDatabase> cards = [];
		foreach (var userCardRelation in userCardRelations)
		{
			_logger.Debug($"Getting card \"{userCardRelation.CardId}\" for user \"{handler.AuthorizedUser.Username}\"");
			var card = cardRepository.GetCardById(userCardRelation.CardId);
			if (card == null)
			{
				return new Result(false, "Error while getting the cards", statusCode: 500);
			}
			cards.Add(new UserCardsDatabase()
			{
				Id = card.Id,
				UUID = card.UUID,
				Type = card.Type,
				Element = card.Element,
				Rarity = card.Rarity,
				Name = card.Name,
				Description = card.Description,
				Damage = card.Damage,
				Race = card.Race,
				Quantity = userCardRelation.Quantity,
				LockedAmount = userCardRelation.LockedAmount
			});
		}

		_logger.Debug($"Returning {cards.Count} cards for user {handler.AuthorizedUser.Username}");

		return new Result(true, JsonSerializer.Serialize(cards), HelperService.APPL_JSON, statusCode: 200);
	}

	/**
	 * Checks if a card is available for a user.
	 *	<param name="cardId">The id of the card</param>
	 *	<param name="userId">The id of the user</param>
	 *	<returns>True if the card is available for the user, otherwise false</returns>
	 */
	public bool IsCardAvailableForUser(int cardId, int userId)
	{
		_logger.Debug($"IsCardAvailableForUser - Checking if card \"{cardId}\" is available for user \"{userId}\"");
		var relation = cardRepository.GetUserCardRelation(userId, cardId);
		return relation != null && relation.Quantity > relation.LockedAmount;
	}
}