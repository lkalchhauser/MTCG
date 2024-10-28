using System.Text.Json.Serialization;

namespace MTCG.Server.Util.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Race
{
    GOBLIN,
    DRAGON,
    WIZARD,
    ORK,
    KNIGHT,
    KRAKEN,
    FIRE_ELVES
}