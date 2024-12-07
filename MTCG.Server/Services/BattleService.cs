using System.Collections.Concurrent;
using MTCG.Server.HTTP;
using MTCG.Server.Util;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services;

public class BattleService
{
	private readonly ConcurrentQueue<TaskCompletionSource<(Handler, Result)>> _waitingPlayers = new();

	public async Task<Result> WaitForBattleAsync(Handler handler, TimeSpan timeout)
	{
		var tcs = new TaskCompletionSource<(Handler, Result)>();

		_waitingPlayers.Enqueue(tcs);

		if (_waitingPlayers.Count >= 2)
		{
			if (_waitingPlayers.TryDequeue(out var player1) &&
			    _waitingPlayers.TryDequeue(out var player2))
			{
				//TODO: proper battle logic
				var result1 = new Result(true, "Battle successful!", Helper.TEXT_PLAIN);
				var result2 = new Result(true, "Battle lost!", Helper.TEXT_PLAIN);


				player1.SetResult((handler, result1));
				player2.SetResult((handler, result2));
			}
		}

		var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
		if (completedTask == tcs.Task)
		{
			var (_, result) = await tcs.Task;
			return result; // Paired successfully
		}
		else
		{
			// Timeout occurred, clean up the queue
			if (_waitingPlayers.TryDequeue(out var remainingPlayer) && remainingPlayer == tcs)
			{
				var timeoutResult = new Result(false, "Timeout: No opponent found.", Helper.TEXT_PLAIN);
				tcs.SetResult((handler, timeoutResult));
			}

			return new Result(false, "Timeout: No opponent found.", Helper.TEXT_PLAIN);
		}
	}
}