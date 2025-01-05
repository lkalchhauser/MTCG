using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;
using System.Text.Json;

namespace MTCG.Server.Services;

public class TransactionService(
	IPackageRepository packageRepository,
	ICardRepository cardRepository,
	IUserRepository userRepository,
	ICardService cardService)
	: ITransactionService
{
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	public Result GetRandomPackageForUser(IHandler handler)
	{
		var pckgId = packageRepository.GetRandomPackageId();
		if (pckgId == 0)
		{
			_logger.Debug("GetRandomPackageForUser - No packages found");
			return new Result(false, "No packages found!", statusCode: 404);
		}
		var package = packageRepository.GetPackageWithoutCardsById(pckgId);

		if (handler.AuthorizedUser.Coins < package.Cost)
		{
			_logger.Debug("GetRandomPackageForUser - Not enough coins");
			return new Result(false, "Not enough coins!", statusCode: 403);
		}

		var packageCardIds = packageRepository.GetPackageCardIds(pckgId);

		foreach (var cardId in packageCardIds)
		{
			cardService.AddCardToUserStack(handler.AuthorizedUser.Id, cardId);
		}

		handler.AuthorizedUser.Coins -= package.Cost;
		userRepository.UpdateUser(handler.AuthorizedUser);
		RemoveOnePackageById(package.Id);

		List<Card> cards = [];

		cards.AddRange(packageCardIds.Select(cardRepository.GetCardById).OfType<Card>());

		return new Result(true, JsonSerializer.Serialize(cards), contentType: HelperService.APPL_JSON, statusCode: 200);
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