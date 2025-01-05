using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.Enums;
using NSubstitute;
using NUnit.Framework;
using System.Text.Json;

namespace MTCG.Tests.Services;

[TestFixture]
public class TradeServiceTests
{
	private ITradeRepository _tradeRepository;
	private ICardRepository _cardRepository;
	private IUserRepository _userRepository;
	private ICardService _cardService;
	private IDeckService _deckService;
	private TradeService _tradeService;
	private IHandler _handler;

	[SetUp]
	public void Setup()
	{
		_tradeRepository = Substitute.For<ITradeRepository>();
		_cardRepository = Substitute.For<ICardRepository>();
		_userRepository = Substitute.For<IUserRepository>();
		_cardService = Substitute.For<ICardService>();
		_deckService = Substitute.For<IDeckService>();

		_tradeService = new TradeService(
			_tradeRepository,
			_cardRepository,
			_userRepository,
			_cardService,
			_deckService
		);

		_handler = TestHelper.CreateMockHandler("username", 1);
	}

	[Test]
	public void CreateTradeOffer_ShouldReturnErrorIfNoPayload()
	{
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns((string)null);

		var result = _tradeService.CreateTradeOffer(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
	}

	[Test]
	public void CreateTradeOffer_ShouldReturnErrorIfCardNotFound()
	{
		var tradeOffer = TestHelper.CreateSimpleTradeOffer(1, 1, 1);
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns(JsonSerializer.Serialize(tradeOffer));

		_cardRepository.GetCardByUuid(tradeOffer.CardUUID).Returns((Card)null);

		var result = _tradeService.CreateTradeOffer(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
	}

	[Test]
	public void CreateTradeOffer_ShouldReturnErrorIfCardNotOwned()
	{
		var tradeOffer = TestHelper.CreateSimpleTradeOffer(1, 1, 1);
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns(JsonSerializer.Serialize(tradeOffer));

		var card = TestHelper.CreateSimpleCard(1);
		_cardRepository.GetCardByUuid(tradeOffer.CardUUID).Returns(card);
		_cardRepository.GetUserCardRelation(1, card.Id).Returns((UserCardRelation)null);

		var result = _tradeService.CreateTradeOffer(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(403));
	}

	[Test]
	public void CreateTradeOffer_ShouldReturnSuccessIfTradeOfferIsCreated()
	{
		var tradeOffer = TestHelper.CreateSimpleTradeOffer(1, 1, 1);
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns(JsonSerializer.Serialize(tradeOffer));

		var card = TestHelper.CreateSimpleCard(1, name: "Test Card");
		_cardRepository.GetCardByUuid(tradeOffer.CardUUID).Returns(card);
		_cardRepository.GetUserCardRelation(1, card.Id).Returns(new UserCardRelation() { Quantity = 1, LockedAmount = 0 });

		_tradeRepository.AddTradeOffer(Arg.Any<TradeOffer>()).Returns(true);

		var result = _tradeService.CreateTradeOffer(_handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(201));
	}

	[Test]
	public void GetCurrentlyActiveTrades_ShouldReturnNoTradesFound()
	{
		_tradeRepository.GetAllTradesWithStatus(TradeStatus.ACTIVE).Returns((List<TradeOffer>)null);

		var result = _tradeService.GetCurrentlyActiveTrades(_handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(204));
	}

	[Test]
	public void GetCurrentlyActiveTrades_ShouldReturnSuccessIfTradesExist()
	{
		var activeTrades = new List<TradeOffer>
		{
			new() { Id = 1, CardId = 1, UserId = 1, DesiredCardType = CardType.MONSTER, DesiredCardRarity = Rarity.LEGENDARY },
			new() { Id = 2, CardId = 2, UserId = 2, DesiredCardType = CardType.SPELL, DesiredCardElement = Element.AIR, DesiredCardMinimumDamage = 10 }
		};

		_tradeRepository.GetAllTradesWithStatus(TradeStatus.ACTIVE).Returns(activeTrades);

		var result = _tradeService.GetCurrentlyActiveTrades(_handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo(JsonSerializer.Serialize(activeTrades)));
	}

	[Test]
	public void DeleteTrade_ShouldReturnErrorIfTradeNotFound()
	{
		_handler.Path.Returns("/trades/1");
		_tradeRepository.GetTradeById(1).Returns((TradeOffer)null);

		var result = _tradeService.DeleteTrade(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(404));
	}

	[Test]
	public void DeleteTrade_ShouldReturnErrorIfNotOwner()
	{
		var trade = TestHelper.CreateSimpleTradeOffer(1, 1, 2);
		_handler.Path.Returns("/trades/1");
		_tradeRepository.GetTradeById(1).Returns(trade);

		var result = _tradeService.DeleteTrade(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(403));
	}

	[Test]
	public void DeleteTrade_ShouldReturnSuccessIfTradeIsDeleted()
	{
		_handler.Path.Returns("/trades/1");

		var trade = TestHelper.CreateSimpleTradeOffer(1, 1, 1);
		_tradeRepository.GetTradeById(1).Returns(trade);

		_tradeRepository.UpdateTrade(Arg.Any<TradeOffer>()).Returns(true);

		var result = _tradeService.DeleteTrade(_handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo("Trade successfully deleted!"));
	}

	[Test]
	public void AcceptTradeOffer_ShouldReturnErrorIfTradeNotFound()
	{
		_handler.Path.Returns("/trades/1");
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns("{}");
		_tradeRepository.GetTradeById(1).Returns((TradeOffer)null);

		var result = _tradeService.AcceptTradeOffer(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(404));
	}

	[Test]
	public void AcceptTradeOffer_ShouldReturnErrorIfCardNotFound()
	{
		var tradeAcceptRequest = new TradeAcceptRequest { UUID = "non-existing-card" };
		_handler.Path.Returns("/trades/1");
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns(JsonSerializer.Serialize(tradeAcceptRequest));

		var trade = new TradeOffer() { Id = 1, UserId = 2, Status = TradeStatus.ACTIVE, CardId = 1 };
		_tradeRepository.GetTradeById(1).Returns(trade);
		_cardRepository.GetCardByUuid(tradeAcceptRequest.UUID).Returns((Card)null);

		var result = _tradeService.AcceptTradeOffer(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
	}

	[Test]
	public void AcceptTradeOffer_ShouldFailIfTradeWithSelf()
	{
		var tradeAcceptRequest = new TradeAcceptRequest { UUID = "card-uuid-accepted" };
		_handler.Path.Returns("/trades/1");
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns(JsonSerializer.Serialize(tradeAcceptRequest));

		var trade = TestHelper.CreateSimpleTradeOffer(1, 1, 1);
		_tradeRepository.GetTradeById(1).Returns(trade);

		var offerCard = TestHelper.CreateSimpleCard(1);
		var acceptCard = TestHelper.CreateSimpleCard(2);

		_cardRepository.GetCardByUuid(tradeAcceptRequest.UUID).Returns(acceptCard);
		_cardRepository.GetCardById(trade.CardId).Returns(offerCard);

		_cardService.IsCardAvailableForUser(acceptCard.Id, _handler.AuthorizedUser.Id).Returns(true);
		_cardService.RemoveCardFromUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);
		_cardService.AddCardToUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);
		_cardService.UnlockCardInUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);

		_tradeRepository.UpdateTrade(Arg.Any<TradeOffer>()).Returns(true);
		_tradeRepository.AddTradeAcceptEntry(Arg.Any<TradeAccept>()).Returns(true);

		var result = _tradeService.AcceptTradeOffer(_handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(403));
		Assert.That(result.Message, Is.EqualTo("You cannot trade with yourself!"));
	}

	[Test]
	public void AcceptTradeOffer_ShouldReturnSuccessIfTradeIsAccepted()
	{
		var tradeAcceptRequest = new TradeAcceptRequest { UUID = "card-uuid-accepted" };
		_handler.Path.Returns("/trades/1");
		_handler.GetContentType().Returns(HelperService.APPL_JSON);
		_handler.Payload.Returns(JsonSerializer.Serialize(tradeAcceptRequest));

		var trade = TestHelper.CreateSimpleTradeOffer(1, 1, 2);
		_tradeRepository.GetTradeById(1).Returns(trade);

		var offerCard = TestHelper.CreateSimpleCard(1);
		var acceptCard = TestHelper.CreateSimpleCard(2);

		_cardRepository.GetCardByUuid(tradeAcceptRequest.UUID).Returns(acceptCard);
		_cardRepository.GetCardById(trade.CardId).Returns(offerCard);

		_cardService.IsCardAvailableForUser(acceptCard.Id, _handler.AuthorizedUser.Id).Returns(true);
		_cardService.RemoveCardFromUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);
		_cardService.AddCardToUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);
		_cardService.UnlockCardInUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);

		_tradeRepository.UpdateTrade(Arg.Any<TradeOffer>()).Returns(true);
		_tradeRepository.AddTradeAcceptEntry(Arg.Any<TradeAccept>()).Returns(true);

		var result = _tradeService.AcceptTradeOffer(_handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo("Trade successfully accepted!"));
	}
}