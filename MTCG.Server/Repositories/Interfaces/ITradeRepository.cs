using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Repositories.Interfaces;

public interface ITradeRepository
{
	public bool AddTradeOffer(TradeOffer tradeOffer);
	public List<TradeOffer>? GetAllTradesWithStatus(TradeStatus status);
	public TradeOffer? GetTradeById(int tradeId);
	public bool UpdateTrade(TradeOffer trade);
	public bool AddTradeAcceptEntry(TradeAccept tradeAcceptObject);
}