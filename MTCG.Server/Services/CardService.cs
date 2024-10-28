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

	public Result CreatePackageAndCards(Handler handler)
	{
		if (handler.GetContentType() != "application/json" || handler.Payload == null)
		{
			_logger.Debug("Register User - No valid payload data found");
			return new Result(false, "Badly formatted data sent!");
		}

		var package = JsonSerializer.Deserialize<Package>(handler.Payload);

		foreach (var packageCard in package.Cards)
		{
			if (_cardRepository.AddCard(packageCard)) continue;

			_logger.Debug($"Failed to add card \"{packageCard.Name}\" to database");
			return new Result(false, "Failed to add card to database!");
		}

		return new Result(false, "Not implemented yet!");
	}
}