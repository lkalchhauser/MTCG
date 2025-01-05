using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util.BattleRules;
using MTCG.Server.Util.Enums;
using MTCG.Server.Util.HelperClasses;
using System.Collections.Concurrent;
using System.Text.Json;
using BattleResult = MTCG.Server.Models.BattleResult;

namespace MTCG.Server.Services;

/**
 * Service for handling battles between players.
 */
public class BattleService(
	IDeckRepository deckRepository,
	IDeckService deckService,
	ICardService cardService,
	IUserService userService)
	: IBattleService
{

	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

	// Queue for players waiting to be paired
	private readonly ConcurrentQueue<(IHandler handler, TaskCompletionSource<Result> tcs)> _waitingPlayers = new();

	/**
	 *	Method that waits for a player to be paired with another player and starts the battle if it happens
	 * <param name="handler">The handler</param>
	 *	<param name="timeout">how long should be waited before canceling the battle for timeout</param>
	 */
	public async Task<Result> WaitForBattleAsync(IHandler handler, TimeSpan timeout)
	{
		var currentUserDeckResult = deckService.GetDeckForCurrentUser(handler, true);

		if (currentUserDeckResult.Message == "")
		{
			_logger.Debug("Deck did not contain enough cards to be queued!");
			return new Result(false, "Deck must contain exactly 4 cards!", HelperService.TEXT_PLAIN, 400);
		}

		var deserializedDeck = new Deck()
		{
			Cards = JsonSerializer.Deserialize<List<Card>>(currentUserDeckResult.Message)
		};

		if (deserializedDeck.Cards != null && deserializedDeck.Cards.Count != 4)
		{
			_logger.Debug("Deck did not contain enough cards to be queued!");
			return new Result(false, "Deck must contain exactly 4 cards!", HelperService.TEXT_PLAIN, 400);
		}

		var tcs = new TaskCompletionSource<Result>();

		_logger.Debug($"Player {handler.AuthorizedUser.Username} queued for battle");
		_waitingPlayers.Enqueue((handler, tcs));

		if (_waitingPlayers.Count >= 2)
		{
			if (_waitingPlayers.TryDequeue(out var player1) &&
				 _waitingPlayers.TryDequeue(out var player2))
			{
				DoBattle(player1, player2);
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
			// Clean up the queue after timeout
			if (_waitingPlayers.TryDequeue(out var remainingPlayer) && remainingPlayer == (handler, tcs))
			{
				_logger.Debug($"Player {handler.AuthorizedUser.Username} removed from queue due to timeout");
				var timeoutResult = new Result(false, "Timeout: No opponent found.", HelperService.TEXT_PLAIN, 408);
				tcs.SetResult(timeoutResult);
			}

			_logger.Debug($"Player {handler.AuthorizedUser.Username} removed from queue due to timeout");
			return new Result(false, "Timeout: No opponent found.", HelperService.TEXT_PLAIN, 408);
		}
	}

	/**
	 *	The actual battle logic
	 *	<param name="player1">Contains the handler and the task completion source for player 1</param>
	 *	<param name="player2">Contains the handler and the task completion source for player 1</param>
	 */
	private void DoBattle((IHandler, TaskCompletionSource<Result>) player1, (IHandler, TaskCompletionSource<Result>) player2)
	{
		_logger.Debug($"Players {player1.Item1.AuthorizedUser.Username} and {player2.Item1.AuthorizedUser.Username} paired for battle");
		// TODO: maybe lock decks so it cannot be edited?
		var player1DeckCards = JsonSerializer.Deserialize<List<Card>>(deckService.GetDeckForCurrentUser(player1.Item1, true).Message);
		var player1DeckId = deckRepository.GetDeckIdFromUserId(player1.Item1.AuthorizedUser.Id);
		var player1DeckBackupCopy = new List<Card>(player1DeckCards);
		var player2DeckCards = JsonSerializer.Deserialize<List<Card>>(deckService.GetDeckForCurrentUser(player2.Item1, true).Message);
		var player2DeckId = deckRepository.GetDeckIdFromUserId(player2.Item1.AuthorizedUser.Id);
		var player2DeckBackupCopy = new List<Card>(player2DeckCards);
		var battleLog = new List<BattleLogEntry>();

		if (player1DeckCards is not { Count: 4 } || player2DeckCards is not { Count: 4 })
		{
			// theoretically this should be unreachable since we check the deck before pairing
			player1.Item2.SetResult(new Result(false, "Deck must contain exactly 4 cards!", HelperService.TEXT_PLAIN, 400));
			player2.Item2.SetResult(new Result(false, "Deck must contain exactly 4 cards!", HelperService.TEXT_PLAIN, 400));
			return;
		}

		var roundCount = 0;

		while (roundCount < 100 && player1DeckCards.Any() && player2DeckCards.Any())
		{
			roundCount++;
			var player1Card = DrawRandomCardFromDeck(player1DeckCards);
			var player2Card = DrawRandomCardFromDeck(player2DeckCards);

			_logger.Debug($"Round {roundCount}: {player1.Item1.AuthorizedUser.Username} plays {player1Card.Name} vs {player2.Item1.AuthorizedUser.Username} plays {player2Card.Name}");

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
				_logger.Debug($"Player {player1.Item1.AuthorizedUser.Username} wins this round!");
				logEntry.Result = BattleLogResult.PLAYER_1_WIN;
				player2DeckCards.Remove(player2Card);
				player1DeckCards.Add(player2Card);
			}
			else if (winner == player2Card)
			{
				_logger.Debug($"Player {player2.Item1.AuthorizedUser.Username} wins this round!");
				logEntry.Result = BattleLogResult.PLAYER_2_WIN;
				player1DeckCards.Remove(player1Card);
				player2DeckCards.Add(player1Card);
			}
			else
			{
				_logger.Debug("This round is a draw!");
				logEntry.Result = BattleLogResult.DRAW;
			}
			battleLog.Add(logEntry);
		}

		if (player1DeckCards.Any() && !player2DeckCards.Any())
		{
			_logger.Debug($"Player {player1.Item1.AuthorizedUser.Username} Victory!");
			deckService.RemoveAndUnlockDeck(player2DeckId, player2.Item1.AuthorizedUser, player2DeckBackupCopy);
			cardService.RemoveCardsFromUserStack(player2.Item1.AuthorizedUser.Id, player2DeckBackupCopy);
			cardService.AddCardsToUserStack(player1.Item1.AuthorizedUser.Id, player2DeckBackupCopy);

			userService.UpdateUserStats(player1.Item1, GetUpdatedUserStatsObject(player1.Item1, eloChange: 3, winsChange: 1));
			userService.UpdateUserStats(player2.Item1, GetUpdatedUserStatsObject(player2.Item1, eloChange: -5, lossChange: 1));

			var player1BattleResult = new BattleResult(Util.Enums.BattleResult.WIN, battleLog);
			player1.Item2.SetResult(GetResult(player1.Item1, player1BattleResult));
			var player2BattleResult = new BattleResult(Util.Enums.BattleResult.LOSE, battleLog);
			player2.Item2.SetResult(GetResult(player2.Item1, player2BattleResult));

		}
		else if (player2DeckCards.Any() && !player1DeckCards.Any())
		{
			_logger.Debug($"Player {player2.Item1.AuthorizedUser.Username} Victory!");
			deckService.RemoveAndUnlockDeck(player1DeckId, player1.Item1.AuthorizedUser, player1DeckBackupCopy);
			cardService.RemoveCardsFromUserStack(player1.Item1.AuthorizedUser.Id, player1DeckBackupCopy);
			cardService.AddCardsToUserStack(player2.Item1.AuthorizedUser.Id, player1DeckBackupCopy);

			userService.UpdateUserStats(player1.Item1, GetUpdatedUserStatsObject(player1.Item1, eloChange: -5, lossChange: 1));
			userService.UpdateUserStats(player2.Item1, GetUpdatedUserStatsObject(player2.Item1, eloChange: 3, winsChange: 1));

			var player1BattleResult = new BattleResult(Util.Enums.BattleResult.LOSE, battleLog);
			player1.Item2.SetResult(GetResult(player1.Item1, player1BattleResult));

			var player2BattleResult = new BattleResult(Util.Enums.BattleResult.WIN, battleLog);
			player2.Item2.SetResult(GetResult(player2.Item1, player2BattleResult));
		}
		else
		{
			_logger.Debug("Draw!");
			userService.UpdateUserStats(player1.Item1, GetUpdatedUserStatsObject(player1.Item1, drawsChange: 1));
			userService.UpdateUserStats(player2.Item1, GetUpdatedUserStatsObject(player2.Item1, drawsChange: 1));

			var gameBattleResult = new BattleResult(Util.Enums.BattleResult.DRAW, battleLog);
			player1.Item2.SetResult(GetResult(player1.Item1, gameBattleResult));
			player2.Item2.SetResult(GetResult(player2.Item1, gameBattleResult));
		}
	}

	/**
	 *	Updates a UserStats object with the given values
	 *	<param name="handler">the current handler</param>
	 *	<param name="eloChange">change to elo</param>
	 *	<param name="winsChange">change to wins</param>
	 *	<param name="lossChange">change to losses</param>
	 *	<param name="drawsChange">change to draws</param>
	 *	<returns>The changed user stats</returns>
	 */
	private UserStats GetUpdatedUserStatsObject(IHandler handler, int eloChange = 0, int winsChange = 0, int lossChange = 0, int drawsChange = 0)
	{
		var userStats = JsonSerializer.Deserialize<UserStats>(userService.GetUserStats(handler).Message);
		userStats.Elo += eloChange;
		userStats.Wins += winsChange;
		userStats.Losses += lossChange;
		userStats.Draws += drawsChange;
		return userStats;
	}

	/**
	 *	Generates a result object based on the handler and the battle result
	 *	<param name="handler">The handler</param>
	 *	<param name="result">The battle result</param>
	 *	<returns>The result object</returns>
	 */
	private Result GetResult(IHandler handler, BattleResult result)
	{
		if (!handler.HasPlainFormat()) return new Result(true, JsonSerializer.Serialize(result), HelperService.APPL_JSON);

		var logTable = result.GenerateBattleLogTable();

		if (result.Result == Util.Enums.BattleResult.DRAW)
		{
			logTable += "\nDraw after 100 rounds!";
		}
		else
		{
			logTable += $"\nYou {result.Result}!";
		}

		return new Result(true, logTable, HelperService.TEXT_PLAIN, 200);
	}

	/**
	 *	Draws a random card from the given deck
	 *	<param name="deck">The deck to draw from</param>
	 *	<returns>The drawn card</returns>
	 */
	private static Card DrawRandomCardFromDeck(List<Card> deck)
	{
		var random = new Random();
		var randomIndex = random.Next(deck.Count);
		return deck[randomIndex];
	}

	/**
	 *	Performs a single round of a fight between two cards
	 *	<param name="card1">The first card</param>
	 *	<param name="card2">The second card</param>
	 *	<returns>The winner of the round and the log message</returns>
	 */
	public (Card? Winner, string Log) FightRound(Card card1, Card card2)
	{
		_logger.Debug($"Fighting round: {card1.Name} vs {card2.Name}");
		if (BattleRules.Apply(card1, card2, out SpecialRuleResult winner))
		{
			_logger.Debug($"Special rule applied: {winner.LogMessage}");
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