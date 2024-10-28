using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class TransactionService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private PackageRepository _packageRepository = new PackageRepository();
	private CardRepository _cardRep = new CardRepository();
	private UserRepository _userRepository = new UserRepository();
	private CardService _cardService = new CardService();


	public Result GetRandomPackageForUser(Handler handler)
	{
		var pckgId = _packageRepository.GetRandomPackageId();
		if (pckgId == 0)
		{
			_logger.Debug("GetRandomPackageForUser - No packages found");
			return new Result(false, "No packages found!");
		}
		var package = _packageRepository.GetPackageWithoutCardsById(pckgId);
		var packageCardIds = _packageRepository.GetPackageCardIds(pckgId);

		foreach (var cardId in packageCardIds)
		{
			_cardService.AddCardToUserStack(handler.AuthorizedUser.Id, cardId);
		}

		handler.AuthorizedUser.Coins -= package.Cost;
		_userRepository.UpdateUser(handler.AuthorizedUser);

		//List<Card> cards = [];

		//cards.AddRange(packageCardIds.Select(cardId => _cardRep.GetCardById(cardId)).OfType<Card>());

		return new Result(true, "");
	}
}