using System.Collections.Concurrent;
using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories;
using MTCG.Server.Util;
using MTCG.Server.Util.BattleRules;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;
using BattleResult = MTCG.Server.Models.BattleResult;

namespace MTCG.Server.Services;

public class BattleService
{
	// TODO: give this from Router instead of new one
	private DeckService _deckService = new DeckService();
	private CardService _cardService = new CardService();
	private UserService _userService = new UserService();
	private DeckRepository _deckRepository = new DeckRepository();
	private readonly ConcurrentQueue<(Handler handler, TaskCompletionSource<Result> tcs)> _waitingPlayers = new();


	public async Task<Result> WaitForBattleAsync(Handler handler, TimeSpan timeout, DeckService deckService, CardService cardService)
	{
		var currentUserDeckResult = deckService.GetDeckForCurrentUser(handler, true);
		var deserializedDeck = new Deck()
		{
			Cards = JsonSerializer.Deserialize<List<Card>>(currentUserDeckResult.Message)
		};

		if (deserializedDeck.Cards != null && deserializedDeck.Cards.Count != 4)
		{
			return new Result(false, "Deck must contain exactly 4 cards!", Helper.TEXT_PLAIN);
		}

		var tcs = new TaskCompletionSource<Result>();

		_waitingPlayers.Enqueue((handler, tcs));

		if (_waitingPlayers.Count >= 2)
		{
			if (_waitingPlayers.TryDequeue(out var player1) &&
			    _waitingPlayers.TryDequeue(out var player2))
			{
				// TODO: maybe this doesnt need a battle return
				DoBattle(player1, player2, deckService);
			}
		}

		var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
		if (completedTask == tcs.Task)
		{
			var result = await tcs.Task;
			return result; // Paired successfully
		}
		else
		{
			// Timeout occurred, clean up the queue
			if (_waitingPlayers.TryDequeue(out var remainingPlayer) && remainingPlayer == (handler, tcs))
			{
				var timeoutResult = new Result(false, "Timeout: No opponent found.", Helper.TEXT_PLAIN);
				tcs.SetResult(timeoutResult);
			}

			return new Result(false, "Timeout: No opponent found.", Helper.TEXT_PLAIN);
		}
	}

	// TODO: when setting the deck after win/lose, add the new cards to user stack and not deck
	private void DoBattle((Handler, TaskCompletionSource<Result>) player1, (Handler, TaskCompletionSource<Result>) player2, DeckService deckService)
	{
		// TODO: maybe lock decks so it cannot be edited?
		var player1DeckCards = JsonSerializer.Deserialize<List<Card>>(deckService.GetDeckForCurrentUser(player1.Item1, true).Message);
		var player1DeckId = _deckRepository.GetDeckIdFromUserId(player1.Item1.AuthorizedUser.Id);
		var player1DeckBackupCopy = new List<Card>(player1DeckCards);
		var player2DeckCards = JsonSerializer.Deserialize<List<Card>>(deckService.GetDeckForCurrentUser(player2.Item1, true).Message);
		var player2DeckId = _deckRepository.GetDeckIdFromUserId(player2.Item1.AuthorizedUser.Id);
		var player2DeckBackupCopy = new List<Card>(player2DeckCards);
		var battleLog = new List<BattleLogEntry>();

		if (player1DeckCards is not { Count: 4 } || player2DeckCards is not { Count: 4 })
		{
			player1.Item2.SetResult(new Result(false, "Deck must contain exactly 4 cards!", Helper.TEXT_PLAIN));
			player2.Item2.SetResult(new Result(false, "Deck must contain exactly 4 cards!", Helper.TEXT_PLAIN));
			return;
		}

		var roundCount = 0;

		while (roundCount < 100 && player1DeckCards.Any() && player2DeckCards.Any())
		{
			roundCount++;
			var player1Card = DrawRandomCardFromDeck(player1DeckCards);
			var player2Card = DrawRandomCardFromDeck(player2DeckCards);

			var (winner, logMessage) = FightRound(player1Card, player2Card);

			var logEntry = new BattleLogEntry()
			{
				Round = roundCount,
				Player1 = player1.Item1.AuthorizedUser.Username,
				Player2 = player2.Item1.AuthorizedUser.Username,
				Card1 = player1Card,
				Card2 = player2Card,
				Message = logMessage
			};

			if (winner == player1Card)
			{
				logEntry.Result = BattleLogResult.PLAYER_1_WIN;
				player2DeckCards.Remove(player2Card);
				player1DeckCards.Add(player2Card);
			}
			else if (winner == player2Card)
			{
				logEntry.Result = BattleLogResult.PLAYER_2_WIN;
				player1DeckCards.Remove(player1Card);
				player2DeckCards.Add(player1Card);
			}
			else
			{
				logEntry.Result = BattleLogResult.DRAW;
			}
			battleLog.Add(logEntry);
		}

		if (player1DeckCards.Any() && !player2DeckCards.Any())
		{
			_deckService.RemoveAndUnlockDeck(player2DeckId, player2.Item1.AuthorizedUser, player2DeckBackupCopy);
			_cardService.RemoveCardsFromUserStack(player2.Item1.AuthorizedUser.Id, player2DeckBackupCopy);
			_cardService.AddCardsToUserStack(player1.Item1.AuthorizedUser.Id, player2DeckBackupCopy);

			_userService.UpdateUserStats(player1.Item1, GetUpdatedUserStatsObject(player1.Item1, eloChange: 3, winsChange: 1));
			_userService.UpdateUserStats(player2.Item1, GetUpdatedUserStatsObject(player2.Item1, eloChange: -5, lossChange: 1));

			var player1BattleResult = new BattleResult(Util.Enums.BattleResult.WIN, battleLog);
			player1.Item2.SetResult(GetResult(player1.Item1, player1BattleResult));
			var player2BattleResult = new BattleResult(Util.Enums.BattleResult.LOSE, battleLog);
			player2.Item2.SetResult(GetResult(player2.Item1, player2BattleResult));

		}
		else if (player2DeckCards.Any() && !player1DeckCards.Any())
		{
			_deckService.RemoveAndUnlockDeck(player1DeckId, player1.Item1.AuthorizedUser, player1DeckBackupCopy);
			_cardService.RemoveCardsFromUserStack(player1.Item1.AuthorizedUser.Id, player1DeckBackupCopy);
			_cardService.AddCardsToUserStack(player2.Item1.AuthorizedUser.Id, player1DeckBackupCopy);

			_userService.UpdateUserStats(player1.Item1, GetUpdatedUserStatsObject(player1.Item1, eloChange: -5, lossChange: 1));
			_userService.UpdateUserStats(player2.Item1, GetUpdatedUserStatsObject(player2.Item1, eloChange: 3, winsChange: 1));

			var player1BattleResult = new BattleResult(Util.Enums.BattleResult.LOSE, battleLog);
			player1.Item2.SetResult(GetResult(player1.Item1, player1BattleResult));

			var player2BattleResult = new BattleResult(Util.Enums.BattleResult.WIN, battleLog);
			player2.Item2.SetResult(GetResult(player2.Item1, player2BattleResult));
		}
		else
		{
			_userService.UpdateUserStats(player1.Item1, GetUpdatedUserStatsObject(player1.Item1, drawsChange: 1));
			_userService.UpdateUserStats(player2.Item1, GetUpdatedUserStatsObject(player2.Item1, drawsChange: 1));

			var gameBattleResult = new BattleResult(Util.Enums.BattleResult.DRAW, battleLog);
			player1.Item2.SetResult(GetResult(player1.Item1, gameBattleResult));
			player2.Item2.SetResult(GetResult(player2.Item1, gameBattleResult));
		}
	}

	private UserStats GetUpdatedUserStatsObject(Handler handler, int eloChange = 0, int winsChange = 0, int lossChange = 0, int drawsChange = 0)
	{
		var userStats = JsonSerializer.Deserialize<UserStats>(_userService.GetUserStats(handler).Message);
		userStats.Elo += eloChange;
		userStats.Wins += winsChange;
		userStats.Losses += lossChange;
		userStats.Draws += drawsChange;
		return userStats;
	}

	private Result GetResult(Handler handler, BattleResult result)
	{
		if (!handler.HasPlainFormat()) return new Result(true, JsonSerializer.Serialize(result), Helper.APPL_JSON);

		var logTable = result.GenerateBattleLogTable();

		if (result.Result == Util.Enums.BattleResult.DRAW)
		{
			logTable += "\nDraw after 100 rounds!";
		}
		else
		{
			logTable += $"\nYou {result.Result}!";
		}
				
		return new Result(true, logTable, Helper.TEXT_PLAIN);
	}

	private static Card DrawRandomCardFromDeck(List<Card> deck)
	{
		var random = new Random();
		var randomIndex = random.Next(deck.Count);
		return deck[randomIndex];
	}

	private (Card? Winner, string Log) FightRound(Card card1, Card card2)
	{
		if (BattleRules.Apply(card1, card2, out SpecialRuleResult winner))
		{
			return (winner.Winner, winner.LogMessage);
		}

		var log = $"{card1.Name} (Dmg: {card1.Damage}) vs {card2.Name} (Dmg: {card2.Damage})";

		if (card1.Damage > card2.Damage)
		{
			return (card1, log += $" - {card1.Name} Wins!");
		}

		return card2.Damage > card1.Damage ? (card2, log += $" - {card2.Name} Wins!") : (null, log + " - Draw!");
	}
}