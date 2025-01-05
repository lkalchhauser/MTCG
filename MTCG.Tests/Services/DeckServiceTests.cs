using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using NSubstitute;
using NUnit.Framework;

namespace MTCG.Tests.Services;

[TestFixture]
public class DeckServiceTests
{
	private IDeckRepository _deckRepository;
	private ICardRepository _cardRepository;
	private IDeckService _deckService;

	[SetUp]
	public void Setup()
	{
		_deckRepository = Substitute.For<IDeckRepository>();
		_cardRepository = Substitute.For<ICardRepository>();
		_deckService = new DeckService(_deckRepository, _cardRepository);
	}

	private Deck CreateDeck(List<Card> cards = null)
	{
		return new Deck
		{
			Cards = cards ?? [TestHelper.CreateSimpleCard(1)]
		};
	}

	[Test]
	public void GetDeckForCurrentUser_WithNoCardsInDeck_Returns204NoContent()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		_deckRepository.GetDeckIdFromUserId(1).Returns(1);
		_deckRepository.GetAllCardIdsFromDeckId(1).Returns([]);

		var result = _deckService.GetDeckForCurrentUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(204));
		Assert.That(result.Message, Is.EqualTo("No cards found in deck!"));
	}

	[Test]
	public void GetDeckForCurrentUser_WithCardsInDeck_ReturnsDeckJson()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		var card = TestHelper.CreateSimpleCard(1);
		var deck = CreateDeck([card]);

		_deckRepository.GetDeckIdFromUserId(1).Returns(1);
		_deckRepository.GetAllCardIdsFromDeckId(1).Returns([card.Id]);
		_cardRepository.GetCardById(card.Id).Returns(card);

		var result = _deckService.GetDeckForCurrentUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo("[{\"Id\":1,\"UUID\":\"uuid1\",\"Type\":\"MONSTER\",\"Element\":\"NORMAL\",\"Rarity\":\"NORMAL\",\"Name\":\"Card1\",\"Description\":null,\"Damage\":10,\"Race\":null}]"));
	}

	[Test]
	public void SetDeckForCurrentUser_WithValidPayload_SuccessfullySetsDeck()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.GetContentType().Returns(HelperService.APPL_JSON);
		handler.Payload.Returns("[\"uuid1\", \"uuid2\", \"uuid3\", \"uuid4\"]");

		var card1 = TestHelper.CreateSimpleCard(1, "uuid1");
		var card2 = TestHelper.CreateSimpleCard(2, "uuid2");
		var card3 = TestHelper.CreateSimpleCard(3, "uuid3");
		var card4 = TestHelper.CreateSimpleCard(4, "uuid4");

		var userCardRelation1 = new UserCardRelation { Quantity = 2, LockedAmount = 0, CardId = 1, UserId = 1 };
		var userCardRelation2 = new UserCardRelation { Quantity = 2, LockedAmount = 0, CardId = 2, UserId = 1 };
		var userCardRelation3 = new UserCardRelation { Quantity = 2, LockedAmount = 0, CardId = 3, UserId = 1 };
		var userCardRelation4 = new UserCardRelation { Quantity = 2, LockedAmount = 0, CardId = 4, UserId = 1 };

		_cardRepository.GetCardByUuid("uuid1").Returns(card1);
		_cardRepository.GetCardByUuid("uuid2").Returns(card2);
		_cardRepository.GetCardByUuid("uuid3").Returns(card3);
		_cardRepository.GetCardByUuid("uuid4").Returns(card4);
		_cardRepository.GetCardById(1).Returns(card1);
		_cardRepository.GetCardById(2).Returns(card2);
		_cardRepository.GetCardById(3).Returns(card3);
		_cardRepository.GetCardById(4).Returns(card4);
		_cardRepository.GetAllCardRelationsForUserId(1).Returns([
			userCardRelation1, userCardRelation2, userCardRelation3, userCardRelation4
		]);

		_deckRepository.GetDeckIdFromUserId(1).Returns(1);
		_deckRepository.AddNewDeckToUserId(1).Returns(1);
		_deckRepository.AddCardToDeck(1, 1).Returns(true);
		_deckRepository.AddCardToDeck(1, 2).Returns(true);
		_deckRepository.AddCardToDeck(1, 3).Returns(true);
		_deckRepository.AddCardToDeck(1, 4).Returns(true);
		_deckRepository.GetAllCardIdsFromDeckId(1).Returns([1, 2, 3, 4]);

		_cardRepository.GetUserCardRelation(1, 1).Returns(userCardRelation1);
		_cardRepository.GetUserCardRelation(1, 2).Returns(userCardRelation2);
		_cardRepository.GetUserCardRelation(1, 3).Returns(userCardRelation3);
		_cardRepository.GetUserCardRelation(1, 4).Returns(userCardRelation4);
		var result = _deckService.SetDeckForCurrentUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
	}

	[Test]
	public void SetDeckForCurrentUser_WithInvalidPayload_Returns400BadRequest()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);
		handler.GetContentType().Returns(HelperService.APPL_JSON);
		handler.Payload.Returns("[\"uuid1\", \"uuid2\"]");

		var result = _deckService.SetDeckForCurrentUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
		Assert.That(result.Message, Is.EqualTo("Badly formatted data sent!"));
	}
}