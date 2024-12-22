using System.Collections.Concurrent;
using System.Text.Json;
using MTCG.Server.HTTP;
using MTCG.Server.Models;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class BattleService
{
	private readonly ConcurrentQueue<(Handler handler, TaskCompletionSource<Result> tcs)> _waitingPlayers = new();


	public async Task<Result> WaitForBattleAsync(Handler handler, TimeSpan timeout, DeckService deckService, CardService cardService)
	{
		var currentUserDeckResult = deckService.GetDeckForCurrentUser(handler);
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
		var player1DeckCards = JsonSerializer.Deserialize<List<Card>>(deckService.GetDeckForCurrentUser(player1.Item1).Message);
		var player1DeckBackupCopy = new List<Card>(player1DeckCards);
		var player2DeckCards = JsonSerializer.Deserialize<List<Card>>(deckService.GetDeckForCurrentUser(player2.Item1).Message);
		var player2DeckBackupCopy = new List<Card>(player2DeckCards);
		var battleLog = new List<string>();

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

			var (winner, log) = FightRound(player1Card, player2Card);
			battleLog.Add($"Round {roundCount}: {log}");

			if (winner == player1Card)
			{
				player2DeckCards.Remove(player2Card);
				player1DeckCards.Add(player2Card);
			}
			else if (winner == player2Card)
			{
				player1DeckCards.Remove(player1Card);
				player2DeckCards.Add(player1Card);
			}
		}

		if (player1DeckCards.Any() && !player2DeckCards.Any())
		{
			battleLog.Add($"{player1.Item1.AuthorizedUser.Username} wins!");
			var player1BattleResult = new BattleResult(Util.Enums.BattleResult.WIN, battleLog);
			player1.Item2.SetResult(new Result(true, JsonSerializer.Serialize(player1BattleResult)));
			var player2BattleResult = new BattleResult(Util.Enums.BattleResult.LOSE, battleLog);
			player2.Item2.SetResult(new Result(true, JsonSerializer.Serialize(player2BattleResult)));

		}
		else if (player2DeckCards.Any() && !player1DeckCards.Any())
		{
			battleLog.Add($"{player2.Item1.AuthorizedUser.Username} wins!");
			var player1BattleResult = new BattleResult(Util.Enums.BattleResult.LOSE, battleLog);
			player1.Item2.SetResult(new Result(true, JsonSerializer.Serialize(player1BattleResult)));
			var player2BattleResult = new BattleResult(Util.Enums.BattleResult.WIN, battleLog);
			player2.Item2.SetResult(new Result(true, JsonSerializer.Serialize(player2BattleResult)));
		}
		else
		{
			battleLog.Add("Draw after 100 rounds!");
			var gameBattleResult = new BattleResult(Util.Enums.BattleResult.DRAW, battleLog);
			player1.Item2.SetResult(new Result(true, JsonSerializer.Serialize(gameBattleResult)));
			player2.Item2.SetResult(new Result(true, JsonSerializer.Serialize(gameBattleResult)));
		}
	}

	private static Card DrawRandomCardFromDeck(List<Card> deck)
	{
		var random = new Random();
		var randomIndex = random.Next(deck.Count);
		return deck[randomIndex];
	}

	private (Card Winner, string Log) FightRound(Card card1, Card card2)
	{
		return (card1, "");
	}

	private static bool SpecialRelationsApply
}