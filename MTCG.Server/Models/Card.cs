using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

public abstract class Card
{
    private CardType _type { get; set; }

    private Element _element { get; set; }

    private Rarity _rarity { get; set; }

    private string _name { get; set; }

    private int _damage { get; set; }

    protected Card()
    {

    }
}