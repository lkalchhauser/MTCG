using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTCG.Server.Models;

namespace MTCG.Server.Util.BattleRules
{
	internal interface IBattleRule
	{
		bool IsMatch(Card card1, Card card2);
		Card? Apply(Card card1, Card card2);
	}
}
