using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util.Enums;
using NSubstitute;

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
			handler.GetContentType().Returns("application/json");
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

		public static Package CreateSimplePackage(int id, string name = "Package1", int cost = 10, int availableAmount = 1)
		{
			return new Package
			{
				Id = id,
				Name = name,
				Cost = cost,
				AvailableAmount = availableAmount
			};
		}

		public static UserCredentials CreateSimpleUser(int id, string username = "User1", int coins = 100, string password = "password123")
		{
			return new UserCredentials
			{
				Id = id,
				Username = username,
				Coins = coins,
				Password = password
			};
		}
	}
}
