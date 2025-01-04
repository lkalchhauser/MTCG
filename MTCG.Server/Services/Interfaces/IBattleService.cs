using MTCG.Server.HTTP;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface IBattleService
{
	public Task<Result> WaitForBattleAsync(IHandler handler, TimeSpan timeout, DeckService deckService,
		CardService cardService);
}