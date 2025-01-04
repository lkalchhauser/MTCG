using MTCG.Server.Util.Enums;

namespace MTCG.Server.Util.HelperClasses;

public class ElementEffectiveness
{
	public static float GetEffectivenessMultiplier(Element card1Element, Element card2Element)
	{
		return (card1Element, card2Element) switch
		{
			(Element.WATER, Element.FIRE) => 2.0f, // Water is strong against fire
			(Element.WATER, Element.EARTH) => 0.5f, // Water is weak against earth
			(Element.WATER, Element.NORMAL) => 0.5f, // Water is weak against normal
			(Element.FIRE, Element.AIR) => 2.0f, // Fire is strong against air
			(Element.FIRE, Element.WATER) => 0.5f, // Fire is weak against water
			(Element.FIRE, Element.NORMAL) => 2.0f, // Fire is strong against normal
			(Element.EARTH, Element.WATER) => 2.0f, // Earth is strong against water
			(Element.EARTH, Element.AIR) => 0.5f, // Earth is weak against air
			(Element.AIR, Element.EARTH) => 2.0f, // Air is strong against earth
			(Element.AIR, Element.FIRE) => 0.5f, // Air is weak against fire
			(Element.NORMAL, Element.WATER) => 2.0f, // Normal is strong against water
			(Element.NORMAL, Element.FIRE) => 0.5f, // Normal is weak against fire
			_ => 1.0f,
		};
	}
}