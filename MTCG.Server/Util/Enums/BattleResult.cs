using System.Text.Json.Serialization;

namespace MTCG.Server.Util.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BattleResult
{
    PLAYER_1_WIN,
    PLAYER_2_WIN,
    DRAW
}