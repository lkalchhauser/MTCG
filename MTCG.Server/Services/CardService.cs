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
}