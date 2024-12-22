using MTCG.Server.Models;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.HelperClasses;

public class BattleLogEntry
{
	public int Round { get; set; }
	public string Player1 { get; set; }
	public string Player2 { get; set; }
	public Card Card1 { get; set; }
	public Card Card2 { get; set; }
	public BattleLogResult Result { get; set; }
	public string Message { get; set; }
}