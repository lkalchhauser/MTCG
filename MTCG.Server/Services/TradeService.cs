using MTCG.Server.Repositories;

namespace MTCG.Server.Services;

public class TradeService
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private CardRepository _cardRepository = new CardRepository();
	private PackageRepository _packageRepository = new PackageRepository();
}