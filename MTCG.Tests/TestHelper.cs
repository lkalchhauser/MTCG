using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using NSubstitute;
using NUnit.Framework;

namespace MTCG.Tests
{
	public static class TestHelper
	{
		public static Card CreateSimpleCard(int id, string uuid = "uuid1", string name = "Card1", float damage = 10f)
		{
			return new Card
			{
				Id = id,
				UUID = uuid,
				Name = name,
				Damage = damage,
				Type = CardType.MONSTER,
				Element = Element.NORMAL,
				Rarity = Rarity.NORMAL
			};
		}

		public static IHandler CreateMockHandler(string username, int id)
		{
			var handler = Substitute.For<IHandler>();
			handler.AuthorizedUser.Returns(new UserCredentials
			{
				Id = id,
				Username = username,
				Coins = 100,
				Password = "password123",
				Token = $"{username}-mtcgToken"
			});
			return handler;
		}

		public static TradeOffer CreateSimpleTradeOffer(int id, int cardId, int userId, string cardUUID = "uuid1", TradeStatus status = TradeStatus.ACTIVE)
		{
			return new TradeOffer
			{
				Id = id,
				CardId = cardId,
				UserId = userId,
				CardUUID = cardUUID,
				Status = status
			};
		}
	}
}
