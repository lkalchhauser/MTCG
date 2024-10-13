using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models.Cards;

public class MonsterCard : Card
{
    public MonsterCard(CardType type, Element cardElement, Rarity rarity, string name, int damage, Race race) : base(type, cardElement, rarity, name, damage)
    {
        _race = race;
    }

    private Race _race { get; set; }
}