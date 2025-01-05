using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface IBattleService
{
	public Task<Result> WaitForBattleAsync(IHandler handler, TimeSpan timeout);

	public (Card? Winner, string Log) FightRound(Card card1, Card card2);
}