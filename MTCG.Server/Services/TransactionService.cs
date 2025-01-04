using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class TransactionService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private IPackageRepository _packageRepository;
	private ICardRepository _cardRep;
	private IUserRepository _userRepository;
	private CardService _cardService;

	public TransactionService(IPackageRepository packageRepository, ICardRepository cardRep, IUserRepository userRepository, CardService cardService)
	{
		_packageRepository = packageRepository;
		_cardRep = cardRep;
		_userRepository = userRepository;
		_cardService = cardService;
	}

	public Result GetRandomPackageForUser(IHandler handler)
	{
		var pckgId = _packageRepository.GetRandomPackageId();
		if (pckgId == 0)
		{
			_logger.Debug("GetRandomPackageForUser - No packages found");
			return new Result(false, "No packages found!");
		}
		var package = _packageRepository.GetPackageWithoutCardsById(pckgId);

		if (handler.AuthorizedUser.Coins < package.Cost)
		{
			_logger.Debug("GetRandomPackageForUser - Not enough coins");
			return new Result(false, "Not enough coins!");
		}

		var packageCardIds = _packageRepository.GetPackageCardIds(pckgId);

		foreach (var cardId in packageCardIds)
		{
			_cardService.AddCardToUserStack(handler.AuthorizedUser.Id, cardId);
		}

		handler.AuthorizedUser.Coins -= package.Cost;
		_userRepository.UpdateUser(handler.AuthorizedUser);
		RemoveOnePackageById(package.Id);

		//List<Card> cards = [];

		//cards.AddRange(packageCardIds.Select(cardId => _cardRep.GetCardById(cardId)).OfType<Card>());

		return new Result(true, "");
	}

	public void RemoveOnePackageById(int packageId)
	{
		var package = _packageRepository.GetPackageWithoutCardsById(packageId);
		if (package.AvailableAmount == 1)
		{
			_packageRepository.DeletePackage(package.Id);
			return;
		}
		package.AvailableAmount--;
		_packageRepository.UpdatePackage(package);
	}
}