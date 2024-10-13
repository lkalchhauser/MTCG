using MTCG.Server.Models.Cards;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.Models;

public class Package
{
    private Card[] _cards;

    private int _cost;

    private Rarity _rarity;
}