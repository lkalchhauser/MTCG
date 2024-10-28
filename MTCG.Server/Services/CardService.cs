using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class CardService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private CardRepository _cardRepository = new CardRepository();
	private PackageRepository _packageRepository = new PackageRepository();

	// TODO: maybe move this to packageservice since it's actually the /package path
	public Result CreatePackageAndCards(Handler handler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Register User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		var package = JsonSerializer.Deserialize<Package>(handler.Payload);
		List<int> cardIds = []; // should always be 5 long
		foreach (var packageCard in package.Cards)
		{
			if (_cardRepository.AddCard(packageCard))
			{
				cardIds.Add(packageCard.Id);
				continue;
			}

			_logger.Debug($"Failed to add card \"{packageCard.Name}\" to database");
			return new Result(false, "Failed to add card to database!");
		}

		_packageRepository.AddPackage(package);

		foreach (var cardId in cardIds)
		{
			
			if (_packageRepository.AddPackageCardRelation(package.Id, cardId)) continue;
			_logger.Debug($"Failed to add card \"{cardId}\" to package \"{package.Id}\"");
			return new Result(false, "Failed to add card to package");
		}

		return new Result(true, "Package successfully added!");
	}

	public bool AddCardToUserStack(int userId, int cardId)
	{
		var relation = _cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return _cardRepository.AddNewCardToUserStack(userId, cardId);
		}

		relation.Quantity++;
		return _cardRepository.UpdateUserStack(relation);
	}

	public bool RemoveCardFromUserStack(int userId, int cardId)
	{
		var relation = _cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return false;
		}

		if ((relation.Quantity - relation.LockedAmount) <= 0) return false;

		if (relation.Quantity == 1)
		{
			_cardRepository.RemoveCardUserStack(relation);
			return true;
		}

		relation.Quantity--;
		_cardRepository.UpdateUserStack(relation);
		return true;
	}

	public bool LockCardInUserStack(int userId, int cardId)
	{
		var relation = _cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return false;
		}

		if (relation.Quantity <= relation.LockedAmount) return false;

		relation.LockedAmount++;
		_cardRepository.UpdateUserStack(relation);
		return true;
	}

	public bool UnlockCardInUserStack(int userId, int cardId)
	{
		var relation = _cardRepository.GetUserCardRelation(userId, cardId);
		if (relation == null)
		{
			return false;
		}

		if (relation is not { LockedAmount: > 0, Quantity: > 0 } || relation.Quantity < relation.LockedAmount)
			return false;

		relation.LockedAmount--;
		_cardRepository.UpdateUserStack(relation);
		return true;

	}
}