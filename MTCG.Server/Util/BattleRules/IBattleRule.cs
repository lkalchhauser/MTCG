using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
