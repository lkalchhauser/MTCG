using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using NSubstitute;
using NUnit.Framework;
using System.Text.Json;

namespace MTCG.Tests.Services;

[TestFixture]
public class TransactionServiceTests
{
	private ITransactionService _transactionService;
	private IPackageRepository _packageRepository;
	private ICardRepository _cardRepository;
	private IUserRepository _userRepository;
	private ICardService _cardService;

	[SetUp]
	public void SetUp()
	{
		_packageRepository = Substitute.For<IPackageRepository>();
		_cardRepository = Substitute.For<ICardRepository>();
		_userRepository = Substitute.For<IUserRepository>();
		_cardService = Substitute.For<ICardService>();

		_transactionService = new TransactionService(_packageRepository, _cardRepository, _userRepository, _cardService);
	}

	[Test]
	public void GetRandomPackageForUser_ShouldReturnSuccessIfPackageIsBought()
	{

		// user starts with 100 coins
		var handler = TestHelper.CreateMockHandler("username", 1);

		var package = TestHelper.CreateSimplePackage(1, "Package1", 50, 5);


		var packageCardIds = new List<int> { 1, 2, 3 };
		var cards = new List<Card>
				{
					 TestHelper.CreateSimpleCard(1, "Card1"),
					 TestHelper.CreateSimpleCard(2, "Card2"),
					 TestHelper.CreateSimpleCard(3, "Card3")
				};

		_packageRepository.GetRandomPackageId().Returns(1);
		_packageRepository.GetPackageWithoutCardsById(1).Returns(package);
		_packageRepository.GetPackageCardIds(1).Returns(packageCardIds);
		_cardRepository.GetCardById(1).Returns(cards[0]);
		_cardRepository.GetCardById(2).Returns(cards[1]);
		_cardRepository.GetCardById(3).Returns(cards[2]);
		_cardService.AddCardToUserStack(Arg.Any<int>(), Arg.Any<int>()).Returns(true);
		_userRepository.UpdateUser(Arg.Any<UserCredentials>()).Returns(true);

		var result = _transactionService.GetRandomPackageForUser(handler);

		Assert.That(result.Success, Is.True);
		Assert.That(result.StatusCode, Is.EqualTo(200));
		Assert.That(result.Message, Is.EqualTo(JsonSerializer.Serialize(cards)));
		Assert.That(handler.AuthorizedUser.Coins, Is.EqualTo(50));
		_packageRepository.Received().UpdatePackage(Arg.Any<Package>());
	}

	[Test]
	public void GetRandomPackageForUser_ShouldReturnErrorIfNotEnoughCoins()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);

		var package = TestHelper.CreateSimplePackage(1, "Package1", 500, 5);

		_packageRepository.GetRandomPackageId().Returns(1);
		_packageRepository.GetPackageWithoutCardsById(1).Returns(package);

		var result = _transactionService.GetRandomPackageForUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(403));
		Assert.That(result.Message, Is.EqualTo("Not enough coins!"));
		_packageRepository.DidNotReceive().GetPackageCardIds(Arg.Any<int>());
		_cardService.DidNotReceive().AddCardToUserStack(Arg.Any<int>(), Arg.Any<int>());
	}

	[Test]
	public void GetRandomPackageForUser_ShouldReturnErrorIfNoPackagesFound()
	{
		var handler = TestHelper.CreateMockHandler("username", 1);

		_packageRepository.GetRandomPackageId().Returns(0);

		var result = _transactionService.GetRandomPackageForUser(handler);

		Assert.That(result.Success, Is.False);
		Assert.That(result.StatusCode, Is.EqualTo(404));
		Assert.That(result.Message, Is.EqualTo("No packages found!"));
		_packageRepository.DidNotReceive().GetPackageWithoutCardsById(Arg.Any<int>());
		_cardRepository.DidNotReceive().GetCardById(Arg.Any<int>());
	}

	[Test]
	public void RemoveOnePackageById_ShouldDeletePackageWhenAvailableAmountIsOne()
	{
		var package = TestHelper.CreateSimplePackage(1);

		_packageRepository.GetPackageWithoutCardsById(1).Returns(package);
		_packageRepository.DeletePackage(1).Returns(true);

		_transactionService.RemoveOnePackageById(1);

		_packageRepository.Received().DeletePackage(1);
		_packageRepository.DidNotReceive().UpdatePackage(Arg.Any<Package>());
	}

	[Test]
	public void RemoveOnePackageById_ShouldDecrementAvailableAmountIfMoreThanOneLeft()
	{
		var package = TestHelper.CreateSimplePackage(1, "Package1", 50, 5);

		_packageRepository.GetPackageWithoutCardsById(1).Returns(package);
		_packageRepository.UpdatePackage(Arg.Any<Package>()).Returns(true);

		_transactionService.RemoveOnePackageById(1);

		Assert.That(package.AvailableAmount, Is.EqualTo(4));
		_packageRepository.Received().UpdatePackage(package);
	}
}