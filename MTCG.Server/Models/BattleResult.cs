using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Models;

public class BattleResult(Util.Enums.BattleResult result, List<BattleLogEntry> battleLog)
{
	public Util.Enums.BattleResult Result { get; set; } = result;
	public List<BattleLogEntry> BattleLog { get; set; } = battleLog;

	public string GenerateBattleLogTable()
	{
		var p1 = BattleLog.First().Player1;
		var p2 = BattleLog.First().Player2;
		var headers = new[] { "Round", $"{p1} Card", $"{p2} Card", "Winner", "Message" };

		var roundWidth = Math.Max(headers[0].Length, BattleLog.Max(e => e.Round.ToString().Length));
		var p1Width = Math.Max(headers[1].Length, BattleLog.Max(e => e.Card1.Name.Length));
		var p2Width = Math.Max(headers[2].Length, BattleLog.Max(e => e.Card2.Name.Length));
		var winnerWidth = Math.Max(headers[3].Length, BattleLog.Max(e => e.Result.ToString().Length));
		var messageWidth = Math.Max(headers[4].Length, BattleLog.Max(e => e.Message.Length));

		var headerRow =
			$"{headers[0].PadRight(roundWidth)} | {headers[1].PadRight(p1Width)} | {headers[2].PadRight(p2Width)} | {headers[3].PadRight(winnerWidth)} | {headers[4].PadRight(messageWidth)}";
		var separatorRow = new string('-', headerRow.Length);

		// Winner shows winning card
		var rows = BattleLog.Select(e =>
			$"{e.Round.ToString().PadRight(roundWidth)} | {e.Card1.Name.PadRight(p1Width)} | {e.Card2.Name.PadRight(p2Width)} | {GetWinnerCardNameFromResult(e.Result, e.Card1, e.Card2).PadRight(winnerWidth)} | {e.Message.PadRight(messageWidth)}"
		);
		// Winner shows winning user
		//var rows = BattleLog.Select(e =>
		//	$"{e.Round.ToString().PadRight(roundWidth)} | {e.Card1.Name.PadRight(p1Width)} | {e.Card2.Name.PadRight(p2Width)} | {GetPlayerNameFromResult(e.Result).PadRight(winnerWidth)} | {e.Message.PadRight(messageWidth)}"
		//);

		var finalTable = $"{headerRow}\n{separatorRow}\n{string.Join("\n", rows)}";

		return finalTable;
	}

	private string GetWinnerCardNameFromResult(BattleLogResult battleLogResult, Card card1, Card card2)
	{
		return battleLogResult switch
		{
			BattleLogResult.PLAYER_1_WIN => card1.Name,
			BattleLogResult.PLAYER_2_WIN => card2.Name,
			_ => "Draw"
		};
	}

	private string GetPlayerNameFromResult(BattleLogResult battleLogResult)
	{
		return battleLogResult switch
		{
			BattleLogResult.PLAYER_1_WIN => BattleLog.First().Player1,
			BattleLogResult.PLAYER_2_WIN => BattleLog.First().Player2,
			_ => "Draw"
		};
	}
}