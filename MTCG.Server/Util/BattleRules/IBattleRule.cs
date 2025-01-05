using MTCG.Server.Models;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Util.BattleRules
{
	internal interface IBattleRule
	{
		bool IsMatch(Card card1, Card card2);
		SpecialRuleResult Apply(Card card1, Card card2);
	}
}
