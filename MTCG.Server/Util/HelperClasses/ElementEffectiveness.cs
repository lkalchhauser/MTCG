using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.HelperClasses;

public class ElementEffectiveness
{
	public static float GetEffectivenessMultiplier(Element card1Element, Element card2Element)
	{
		return (card1Element, card2Element) switch
		{
			(Element.WATER, Element.FIRE) => 2.0f, // Water is strong against fire
			(Element.FIRE, Element.NORMAL) => 2.0f, // Fire is strong against normal
			(Element.NORMAL, Element.WATER) => 2.0f, // Normal is strong against water
			(Element.FIRE, Element.WATER) => 0.5f, // Fire is weak against water
			(Element.WATER, Element.NORMAL) => 0.5f, // Water is weak against normal
			(Element.NORMAL, Element.FIRE) => 0.5f, // Normal is weak against fire
			_ => 1.0f,
		};
	}
}