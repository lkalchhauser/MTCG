using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;
using NSubstitute;
using NUnit.Framework;

namespace MTCG.Tests.Services;

[TestFixture]
public class BattleServiceTests
{
	private IDeckRepository _deckRepository;
	private IDeckService _deckService;
	private ICardService _cardService;
	private IUserService _userService;
	private BattleService _battleService;

	[SetUp]
	public void Setup()
	{
		_deckRepository = Substitute.For<IDeckRepository>();
		_deckService = Substitute.For<IDeckService>();
		_cardService = Substitute.For<ICardService>();
		_userService = Substitute.For<IUserService>();
		_battleService = new BattleService(_deckRepository, _deckService, _cardService, _userService);
	}

	private Card CreateMockCard(
		int id,
		string name,
		CardType type = CardType.MONSTER,
		Element element = Element.NORMAL,
		float damage = 0,
		Rarity rarity = Rarity.NORMAL,
		Race? race = null,
		string description = "Test Description")
	{
		return new Card
		{
			Id = id,
			UUID = Guid.NewGuid().ToString(),
			Type = type,
			Element = element,
			Race = race,
			Name = name,
			Damage = damage,
			Rarity = rarity,
			Description = description
		};
	}

	// this only creates monster cards since we simply are testing the battle queue/fight logic, special rules are tested in different tests
	private List<Card> CreateDeck(int count, float baseDamage = 10)
	{
		var deck = new List<Card>();
		for (int i = 0; i < count; i++)
		{
			deck.Add(new Card
			{
				Id = i + 1,
				UUID = Guid.NewGuid().ToString(),
				Type = CardType.MONSTER,
				Element = Element.NORMAL,
				Rarity = Rarity.NORMAL,
				Name = $"Card{i + 1}",
				Description = $"This is Card {i + 1}",
				Damage = baseDamage + i
			});
		}
		return deck;
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task WaitForBattleAsync_Player1Wins_ReturnsCorrectResult(bool plainFormat)
	{
		var player1 = TestHelper.CreateMockHandler("Player1", 1);
		var player2 = TestHelper.CreateMockHandler("Player2", 2);
		var player1Deck = CreateDeck(4, 15); // higher damage (winner) cards
		var player2Deck = CreateDeck(4, 10); // lower damage (loser) cards

		_deckService.GetDeckForCurrentUser(player1, true)
			.Returns(new Result(true, JsonSerializer.Serialize(player1Deck), "application/json"));
		_deckService.GetDeckForCurrentUser(player2, true)
			.Returns(new Result(true, JsonSerializer.Serialize(player2Deck), "application/json"));
		_deckRepository.GetDeckIdFromUserId(player1.AuthorizedUser.Id).Returns(1);
		_deckRepository.GetDeckIdFromUserId(player2.AuthorizedUser.Id).Returns(2);

		player1.HasPlainFormat().Returns(plainFormat);
		player2.HasPlainFormat().Returns(plainFormat);

		var userStats = new UserStats() { Draws = 0, Elo = 100, Losses = 0, Wins = 0 };
		var result = new Result(true, JsonSerializer.Serialize(userStats), HelperService.APPL_JSON);

		_userService.GetUserStats(player1).Returns(result);
		_userService.GetUserStats(player2).Returns(result);
		_userService.UpdateUserStats(Arg.Any<Handler>(), Arg.Any<UserStats>()).Returns(new Result(true, ""));


		var task1 = _battleService.WaitForBattleAsync(player1, TimeSpan.FromSeconds(10), _deckService, _cardService);
		var task2 = _battleService.WaitForBattleAsync(player2, TimeSpan.FromSeconds(10), _deckService, _cardService);
		var result1 = await task1;
		var result2 = await task2;

		Assert.That(result1.Success);
		Assert.That(result2.Success);

		if (plainFormat)
		{
			Assert.That(result1.Message.Contains("You WIN!"));
			Assert.That(result2.Message.Contains("You LOSE!"));
		}
		else
		{
			Assert.That(result1.Message.Contains("WIN") || result2.Message.Contains("LOSE"));
		}
	}

	[Test]
	public async Task WaitForBattleAsync_Timeout_ReturnsTimeoutResult()
	{
		var player1 = TestHelper.CreateMockHandler("Player1", 1);
		var player1Deck = CreateDeck(4);

		_deckService.GetDeckForCurrentUser(player1, true)
			.Returns(new Result(true, JsonSerializer.Serialize(player1Deck), "application/json"));

		var result = await _battleService.WaitForBattleAsync(player1, TimeSpan.FromMilliseconds(100), _deckService, _cardService);

		Assert.That(result.Success, Is.False);
		Assert.That(result.Message, Does.Contain("Timeout"));
	}

	[Test]
	public async Task WaitForBattleAsync_InvalidDeck_ReturnsErrorResult()
	{
		var player1 = TestHelper.CreateMockHandler("Player1", 1);
		var invalidDeck = CreateDeck(3);

		_deckService.GetDeckForCurrentUser(player1, true)
			.Returns(new Result(true, JsonSerializer.Serialize(invalidDeck), "application/json"));

		var result = await _battleService.WaitForBattleAsync(player1, TimeSpan.FromSeconds(5), _deckService, _cardService);

		Assert.That(result.Success, Is.False);
		Assert.That(result.Message, Does.Contain("Deck must contain exactly 4 cards"));
	}

	[Test]
	public async Task WaitForBattleAsync_BattleEndsInDraw_ReturnsDrawResult()
	{
		var player1 = TestHelper.CreateMockHandler("Player1", 1);
		var player2 = TestHelper.CreateMockHandler("Player2", 2);
		var player1Deck = CreateDeck(4, 10);
		var player2Deck = CreateDeck(4, 10);

		_deckService.GetDeckForCurrentUser(player1, true)
			.Returns(new Result(true, JsonSerializer.Serialize(player1Deck), "application/json"));
		_deckService.GetDeckForCurrentUser(player2, true)
			.Returns(new Result(true, JsonSerializer.Serialize(player2Deck), "application/json"));
		_deckRepository.GetDeckIdFromUserId(player1.AuthorizedUser.Id).Returns(1);
		_deckRepository.GetDeckIdFromUserId(player2.AuthorizedUser.Id).Returns(2);

		var userStats = new UserStats() { Draws = 0, Elo = 100, Losses = 0, Wins = 0 };
		var result = new Result(true, JsonSerializer.Serialize(userStats), HelperService.APPL_JSON);

		_userService.GetUserStats(player1).Returns(result);
		_userService.GetUserStats(player2).Returns(result);
		_userService.UpdateUserStats(Arg.Any<Handler>(), Arg.Any<UserStats>()).Returns(new Result(true, ""));

		var task1 = _battleService.WaitForBattleAsync(player1, TimeSpan.FromSeconds(5), _deckService, _cardService);
		var task2 = _battleService.WaitForBattleAsync(player2, TimeSpan.FromSeconds(5), _deckService, _cardService);
		var result1 = await task1;
		var result2 = await task2;

		Assert.That(result1.Success, Is.True);
		Assert.That(result2.Success, Is.True);
		Assert.That(result1.Message, Does.Contain("DRAW"));
		Assert.That(result2.Message, Does.Contain("DRAW"));
	}

	[Test]
	public void FightRound_DemonManipulatesHumanRule_AppliesCorrectly()
	{
		var demonCard = CreateMockCard(1, "Satan", race: Race.DEMON, damage: 30);
		var humanCard = CreateMockCard(2, "Jeff", race: Race.HUMAN, damage: 40);

		var (winner, log) = _battleService.FightRound(demonCard, humanCard);

		Assert.That(demonCard, Is.EqualTo(winner));
	}

	[Test]
	public void FightRound_KrakenImmuneToSpellRule_AppliesCorrectly()
	{
		var krakenCard = CreateMockCard(3, "Kraken", race: Race.KRAKEN, damage: 40);
		var spellCard = CreateMockCard(4, "Armageddon", type: CardType.SPELL, damage: 50);

		var (winner, log) = _battleService.FightRound(krakenCard, spellCard);

		Assert.That(krakenCard, Is.EqualTo(winner));
	}

	[Test]
	public void FightRound_GoblinFearsDragonRule_AppliesCorrectly()
	{
		var goblinCard = CreateMockCard(5, "Gobta", race: Race.GOBLIN, damage: 87);
		var dragonCard = CreateMockCard(6, "Veldora", race: Race.DRAGON, damage: 45);

		var (winner, log) = _battleService.FightRound(goblinCard, dragonCard);

		Assert.That(dragonCard, Is.EqualTo(winner));
	}

	[Test]
	public void FightRound_KnightDrownsByWaterSpellRule_AppliesCorrectly()
	{
		var knightCard = CreateMockCard(7, "Sir Lancelot", race: Race.KNIGHT, damage: 35);
		var waterSpellCard = CreateMockCard(8, "Water Drop", type: CardType.SPELL, element: Element.WATER, damage: 10);

		var (winner, log) = _battleService.FightRound(knightCard, waterSpellCard);

		Assert.That(waterSpellCard, Is.EqualTo(winner));
	}

	[Test]
	public void FightRound_FireElvesCanEvadeDragonRule_AppliesCorrectly()
	{
		var fireElfCard = CreateMockCard(9, "Fire Elf", race: Race.FIRE_ELVES, element: Element.FIRE, damage: 30);
		var dragonCard = CreateMockCard(10, "Flame Dragon", race: Race.DRAGON, damage: 50);

		var (winner, log) = _battleService.FightRound(fireElfCard, dragonCard);

		Assert.That(fireElfCard, Is.EqualTo(winner));
	}

	[Test]
	public void FightRound_WizardsCanControlOrks_AppliesCorrectly()
	{
		var wizardCard = CreateMockCard(11, "Gandalf", race: Race.WIZARD, damage: 30);
		var orkCard = CreateMockCard(12, "Ork Lord", race: Race.ORK, damage: 50);

		var (winner, log) = _battleService.FightRound(wizardCard, orkCard);

		Assert.That(wizardCard, Is.EqualTo(winner));
	}

	[Test]
	public void FightRound_SlimesAreInvincibleAgainstOni_AppliesCorrectly()
	{
		var slimeCard = CreateMockCard(13, "Your Favorite Slime", race: Race.SLIME, damage: 30);
		var oniCard = CreateMockCard(14, "Benimaru", race: Race.ONI, damage: 50);

		var (winner, log) = _battleService.FightRound(slimeCard, oniCard);

		Assert.That(slimeCard, Is.EqualTo(winner));
	}


	[TestCase(Element.WATER, Element.FIRE, 10, 15, true)]
	[TestCase(Element.WATER, Element.EARTH, 10, 7, false)]
	[TestCase(Element.WATER, Element.NORMAL, 10, 7, false)]
	[TestCase(Element.FIRE, Element.AIR, 10, 15, true)]
	[TestCase(Element.FIRE, Element.WATER, 15, 10, false)]
	[TestCase(Element.FIRE, Element.NORMAL, 10, 15, true)]
	[TestCase(Element.EARTH, Element.WATER, 10, 15, true)]
	[TestCase(Element.EARTH, Element.AIR, 15, 10, false)]
	[TestCase(Element.AIR, Element.EARTH, 10, 15, true)]
	[TestCase(Element.AIR, Element.FIRE, 15, 10, false)]
	[TestCase(Element.NORMAL, Element.WATER, 10, 15, true)]
	[TestCase(Element.NORMAL, Element.FIRE, 15, 10, false)]
	public void FightRound_ElementEffectivenessRule_AppliesCorrectly(Element card1Element, Element card2Element, float card1Damage, float card2Damage, bool card1Wins)
	{
		var card1 = CreateMockCard(15, "Card 1", element: card1Element, damage: card1Damage, type: CardType.SPELL);
		var card2 = CreateMockCard(16, "Card 2", element: card2Element, damage: card2Damage);

		var (winner, log) = _battleService.FightRound(card1, card2);

		Assert.That(card1Wins ? card1 : card2, Is.EqualTo(winner));
	}

}