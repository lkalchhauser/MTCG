namespace MTCG.Server.Models;

public class BattleResult(Util.Enums.BattleResult result, List<string> battleLog)
{
	public Util.Enums.BattleResult Result { get; set; } = result;
	public List<string> BattleLog { get; set; } = battleLog;
}