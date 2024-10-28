using System.Text.Json.Serialization;

namespace MTCG.Server.Util.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardType
{
    MONSTER,
    SPELL
}