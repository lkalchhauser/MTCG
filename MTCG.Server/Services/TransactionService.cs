using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class TransactionService(
	IPackageRepository packageRepository,
	ICardRepository cardRep,
	IUserRepository userRepository,
	ICardService cardService)
	: ITransactionService
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private ICardRepository _cardRep = cardRep;

	public Result GetRandomPackageForUser(IHandler handler)
	{
		var pckgId = packageRepository.GetRandomPackageId();
		if (pckgId == 0)
		{
			_logger.Debug("GetRandomPackageForUser - No packages found");
			return new Result(false, "No packages found!");
		}
		var package = packageRepository.GetPackageWithoutCardsById(pckgId);

		if (handler.AuthorizedUser.Coins < package.Cost)
		{
			_logger.Debug("GetRandomPackageForUser - Not enough coins");
			return new Result(false, "Not enough coins!");
		}

		var packageCardIds = packageRepository.GetPackageCardIds(pckgId);

		foreach (var cardId in packageCardIds)
		{
			cardService.AddCardToUserStack(handler.AuthorizedUser.Id, cardId);
		}

		handler.AuthorizedUser.Coins -= package.Cost;
		userRepository.UpdateUser(handler.AuthorizedUser);
		RemoveOnePackageById(package.Id);

		//List<Card> cards = [];

		//cards.AddRange(packageCardIds.Select(cardId => _cardRep.GetCardById(cardId)).OfType<Card>());

		return new Result(true, "");
	}

	public void RemoveOnePackageById(int packageId)
	{
		var package = packageRepository.GetPackageWithoutCardsById(packageId);
		if (package.AvailableAmount == 1)
		{
			packageRepository.DeletePackage(package.Id);
			return;
		}
		package.AvailableAmount--;
		packageRepository.UpdatePackage(package);
	}
}