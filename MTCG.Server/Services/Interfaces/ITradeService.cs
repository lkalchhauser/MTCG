using MTCG.Server.HTTP;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface ITradeService
{
	public Result CreateTradeOffer(IHandler handler);
	public Result GetCurrentlyActiveTrades(IHandler handler);
	public Result DeleteTrade(IHandler handler);
	public Result AcceptTradeOffer(IHandler handler);
}