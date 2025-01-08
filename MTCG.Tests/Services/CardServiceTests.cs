using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using NSubstitute;
using NUnit.Framework;

namespace MTCG.Tests.Services;

[TestFixture]
public class CardServiceTests
{
	private ICardRepository _cardRepository;
	private IPackageRepository _packageRepository;
	private ICardService _cardService;

	[SetUp]
	public void Setup()
	{
		_cardRepository = Substitute.For<ICardRepository>();
		_packageRepository = Substitute.For<IPackageRepository>();
		_cardService = new CardService(_cardRepository, _packageRepository);
	}

	private Package CreatePackage(string name = "TestPackage", List<Card>? cards = null)
	{
		return new Package
		{
			Name = name,
			Cards = cards ?? [TestHelper.CreateSimpleCard(1)]
		};
	}

	[Test]
	public void CreatePackageAndCards_WithValidPackage_CreatesPackageSuccessfully()
	{

		var handler = Substitute.For<IHandler>();
		handler.GetContentType().Returns("application/json");
		handler.Payload.Returns("{\"Name\":\"TestPackage\",\"Cards\":[{\"UUID\":\"uuid1\",\"Name\":\"Card1\"}]}");

		_packageRepository.GetPackageIdByName("TestPackage").Returns((Package)null);
		_cardRepository.GetCardByUuid("uuid1").Returns((Card)null);

		_cardRepository.AddCard(Arg.Any<Card>()).Returns(1);
		_packageRepository.AddPackage(Arg.Any<Package>()).Returns(true);
		_packageRepository.AddPackageCardRelation(Arg.Any<int>(), Arg.Any<int>()).Returns(true);

		var result = _cardService.CreatePackageAndCards(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(201));
		Assert.That(result.Message, Is.EqualTo("Package successfully added!"));
	}

	[Test]
	public void CreatePackageAndCards_WithExistingPackage_IncreasesAvailableAmount()
	{
		var handler = Substitute.For<IHandler>();
		handler.GetContentType().Returns("application/json");
		handler.Payload.Returns("{\"Name\":\"TestPackage\",\"Cards\":[{\"UUID\":\"uuid1\",\"Name\":\"Card1\"}]}");

		var existingPackage = CreatePackage();
		existingPackage.AvailableAmount = 1;

		_packageRepository.GetPackageIdByName("TestPackage").Returns(existingPackage);

		_cardRepository.GetCardByUuid("uuid1").Returns((Card)null);
		_cardRepository.AddCard(Arg.Any<Card>()).Returns(1);

		var result = _cardService.CreatePackageAndCards(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo("Package with this name already exists, increased available amount!"));
	}

	[Test]
	public void CreatePackageAndCards_WithInvalidPayload_ReturnsBadRequest()
	{
		var handler = Substitute.For<IHandler>();
		handler.GetContentType().Returns("application/json");
		handler.Payload.Returns((string)null);

		var result = _cardService.CreatePackageAndCards(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(400));
		Assert.That(result.Message, Is.EqualTo("Badly formatted data sent!"));
	}

	[Test]
	public void AddCardsToUserStack_WithValidCards_AddsCardsToUser()
	{
		var userId = 1;
		var cards = new List<Card>
		{
			TestHelper.CreateSimpleCard(1),
			TestHelper.CreateSimpleCard(2, "uuid2", "Card2")
		};

		_cardRepository.GetUserCardRelation(userId, 1).Returns((UserCardRelation)null);
		_cardRepository.GetUserCardRelation(userId, 2).Returns((UserCardRelation)null);

		_cardRepository.AddNewCardToUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);

		var result = _cardService.AddCardsToUserStack(userId, cards);

		Assert.That(result, Is.True);
		_cardRepository.Received(1).AddNewCardToUserStack(userId, 1);
		_cardRepository.Received(1).AddNewCardToUserStack(userId, 2);
	}

	[Test]
	public void RemoveCardFromUserStack_WithValidCard_RemovesCardSuccessfully()
	{
		var userId = 1;
		var cardId = 1;
		var relation = new UserCardRelation { Quantity = 2, LockedAmount = 0 };
		_cardRepository.GetUserCardRelation(userId, cardId).Returns(relation);
		_cardRepository.UpdateUserCardRelation(Arg.Any<UserCardRelation>()).Returns(true);

		var result = _cardService.RemoveCardFromUserStack(userId, cardId);

		Assert.That(result, Is.True);
		Assert.That(relation.Quantity, Is.EqualTo(1));
		_cardRepository.Received(1).UpdateUserCardRelation(relation);
	}

	[Test]
	public void RemoveCardFromUserStack_WithValidCardButLocked_ShouldReturnFalse()
	{
		var userId = 1;
		var cardId = 1;
		var relation = new UserCardRelation { Quantity = 2, LockedAmount = 2 };
		_cardRepository.GetUserCardRelation(userId, cardId).Returns(relation);
		_cardRepository.UpdateUserCardRelation(Arg.Any<UserCardRelation>()).Returns(true);

		var result = _cardService.RemoveCardFromUserStack(userId, cardId);

		Assert.That(result, Is.False);
		Assert.That(relation.Quantity, Is.EqualTo(2));
		_cardRepository.Received(0).UpdateUserCardRelation(relation);
	}

	[Test]
	public void LockCardInUserStack_WithValidCard_LocksCardSuccessfully()
	{
		var userId = 1;
		var cardId = 1;
		var relation = new UserCardRelation { Quantity = 2, LockedAmount = 0 };
		_cardRepository.GetUserCardRelation(userId, cardId).Returns(relation);
		_cardRepository.UpdateUserCardRelation(Arg.Any<UserCardRelation>()).Returns(true);

		var result = _cardService.LockCardInUserStack(userId, cardId);

		Assert.That(result, Is.True);
		Assert.That(relation.LockedAmount, Is.EqualTo(1));
		_cardRepository.Received(1).UpdateUserCardRelation(relation);
	}

	[Test]
	public void UnlockCardInUserStack_WithValidCard_UnlocksCardSuccessfully()
	{
		var userId = 1;
		var cardId = 1;
		var relation = new UserCardRelation { Quantity = 2, LockedAmount = 1 };
		_cardRepository.GetUserCardRelation(userId, cardId).Returns(relation);
		_cardRepository.UpdateUserCardRelation(Arg.Any<UserCardRelation>()).Returns(true);

		var result = _cardService.UnlockCardInUserStack(userId, cardId);

		Assert.That(result, Is.True);
		Assert.That(relation.LockedAmount, Is.EqualTo(0));
		_cardRepository.Received(1).UpdateUserCardRelation(relation);
	}

	[Test]
	public void ShowAllCardsForUser_WithNoCards_ReturnsNoCardsMessage()
	{
		var handler = Substitute.For<IHandler>();
		handler.AuthorizedUser.Returns(new UserCredentials() { Coins = 100, Id = 1, Password = "password", Username = "username" });

		_cardRepository.GetAllCardRelationsForUserId(1).Returns([]);

		var result = _cardService.ShowAllCardsForUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(204));
	}

	[Test]
	public void IsCardAvailableForUser_WithAvailableCard_ReturnsTrue()
	{
		var userId = 1;
		var cardId = 1;
		var relation = new UserCardRelation { Quantity = 2, LockedAmount = 0 };
		_cardRepository.GetUserCardRelation(userId, cardId).Returns(relation);

		var result = _cardService.IsCardAvailableForUser(cardId, userId);

		Assert.That(result, Is.True);
	}

	[Test]
	public void IsCardAvailableForUser_WithLockedCard_ReturnsFalse()
	{
		var userId = 1;
		var cardId = 1;
		var relation = new UserCardRelation { Quantity = 1, LockedAmount = 1 };
		_cardRepository.GetUserCardRelation(userId, cardId).Returns(relation);

		var result = _cardService.IsCardAvailableForUser(cardId, userId);

		Assert.That(result, Is.False);
	}
}