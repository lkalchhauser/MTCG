using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MTCG.Server.Models;
using MTCG.Server.Services;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.HTTP;

public class Router
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly DatabaseConnection _dbConnection = DatabaseConnection.Instance;
	private UserService _userService = new UserService();
	private CardService _cardService = new CardService();
	private TransactionService _transactionService = new TransactionService();
	private DeckService _deckService = new DeckService();
	private BattleService _battleService = new BattleService();
	private TradeService _tradingService = new TradeService();

	public async void HandleIncoming(Handler handler)
	{
		// COULD CHANGE THIS TO DIFFERENT ENDPOINTS
		switch (handler.Method)
		{
			case "GET":
				switch(handler.Path)
				{
					case "/":
						_logger.Debug("Routing GET /");
						handler.Reply(200, "Welcome to the Monster Trading Card Game Server!");
						break;
					case { } s when s.StartsWith("/users/"):
						_logger.Debug($"Routing GET {s}");
						if (!Helper.IsUserAuthorized(handler) || !Helper.IsRequestedUserAuthorizedUser(handler))
						{
							handler.Reply(401);
							break;
						}

						var getUserInfoResult = _userService.GetUserInformationForUser(handler);
						handler.Reply(getUserInfoResult.Success ? 200 : 400, getUserInfoResult.Message, getUserInfoResult.ContentType);
						break;
					case "/cards":
						_logger.Debug("Routing GET /cards");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var getCardsResult = _cardService.ShowAllCardsForUser(handler);
						handler.Reply(getCardsResult.Success ? 200 : 400, getCardsResult.Message, getCardsResult.ContentType);
						// return all cards
						break;
					case { } s when s.StartsWith("/deck"):
						_logger.Debug("Routing GET /deck");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var userDeckResult = _deckService.GetDeckForCurrentUser(handler);
						handler.Reply(userDeckResult.Success ? 200 : 400, userDeckResult.Message, userDeckResult.ContentType);
						break;
					case "/stats":
						_logger.Debug("Routing GET /stats");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var userStatsResult = _userService.GetUserStats(handler);
						handler.Reply(userStatsResult.Success ? 200 : 400, userStatsResult.Message, userStatsResult.ContentType);
						break;
					case "/scoreboard":
						_logger.Debug("Routing GET /scoreboard");
						var getScoreboardResult = _userService.GetScoreboard(handler);
						handler.Reply(getScoreboardResult.Success ? 200 : 400, getScoreboardResult.Message, getScoreboardResult.ContentType);
						break;
					case "/tradings":
						_logger.Debug("Routing GET /tradings");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}

						var getAllTradesResult = _tradingService.GetCurrentlyActiveTrades(handler);
						handler.Reply(getAllTradesResult.Success ? 200 : 400, getAllTradesResult.Message, getAllTradesResult.ContentType);
						break;
					default:
						handler.Reply(404);
						break;
				}
				// tradings
				break;
			case "POST":
				switch (handler.Path)
				{
					case "/users":
						_logger.Debug("Routing POST /user");
						var userRegisterResult = _userService.RegisterUser(handler);
						handler.Reply(userRegisterResult.Success ? 201 : 400, userRegisterResult.Message, userRegisterResult.ContentType);
						break;
					case "/sessions":
						_logger.Debug("Routing POST /sessions");
						var userLoginResult = _userService.LoginUser(handler);
						handler.Reply(userLoginResult.Success ? 200 : 400, userLoginResult.Message, userLoginResult.ContentType);
						break;
					case "/packages":
						_logger.Debug("Routing POST /packages");
						var authUser = _userService.GetAuthorizedUserWithToken(handler.GetAuthorizationToken());
						if (authUser is not { Username: "admin" })
						{
							handler.Reply(401);
							return;
						}
						handler.AuthorizedUser = authUser;
						var createPackageResult = _cardService.CreatePackageAndCards(handler);
						handler.Reply(createPackageResult.Success ? 200 : 400, createPackageResult.Message, createPackageResult.ContentType);
						break;
					case "/transactions/packages":
						_logger.Debug("Routing POST /transactions/packages");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var getPackageResult = _transactionService.GetRandomPackageForUser(handler);
						handler.Reply(getPackageResult.Success ? 200 : 400, getPackageResult.Message, getPackageResult.ContentType);
						break;
					case { } s when s.StartsWith("/battles"):
						_logger.Debug("Routing POST /battles");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}

						var battleRequestResult = await _battleService.WaitForBattleAsync(handler, TimeSpan.FromMinutes(1), _deckService, _cardService);
						handler.Reply(battleRequestResult.Success ? 200 : 408, battleRequestResult.Message, battleRequestResult.ContentType);
						break;
					case "/tradings":
						_logger.Debug("Routing POST /tradings");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var createTradingOffer = _tradingService.CreateTradeOffer(handler);
						handler.Reply(createTradingOffer.Success ? 200 : 400, createTradingOffer.Message, createTradingOffer.ContentType);
						break;
					default:
						handler.Reply(404);
						break;
				}
				// tradings/{tradingdealid}
				break;
			case "PUT":
				switch (handler.Path)
				{
					case "/deck":
						_logger.Debug("Routing PUT /deck");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}

						var setDeckResult = _deckService.SetDeckForCurrentUser(handler);
						handler.Reply(setDeckResult.Success ? 200 : 400, setDeckResult.Message, setDeckResult.ContentType);
						break;
					case { } s when s.StartsWith("/users/password"):
						_logger.Debug($"Routing PUT {handler.Path}");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var updatePasswordResult = _userService.UpdatePassword(handler);
						handler.Reply(updatePasswordResult.Success ? 200 : 400, updatePasswordResult.Message, updatePasswordResult.ContentType);
						break;
					case { } s when s.StartsWith("/users/"):
						_logger.Debug($"Routing PUT {handler.Path}");
						if (!Helper.IsUserAuthorized(handler) || !Helper.IsRequestedUserAuthorizedUser(handler))
						{
							handler.Reply(401);
							break;
						}

						var addOrUpdateUserInfoResult = _userService.AddOrUpdateUserInfo(handler);
						handler.Reply(addOrUpdateUserInfoResult.Success ? 200 : 400, addOrUpdateUserInfoResult.Message, addOrUpdateUserInfoResult.ContentType);
						break;
				}
				break;

			// deck
			case "DELETE":
				switch (handler.Path)
				{
					case "/users/userinfo":
						_logger.Debug("Routing DELETE /users/userinfo");
						if (!Helper.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var deleteUserInfoResult = _userService.DeleteUserInfo(handler);
						handler.Reply(deleteUserInfoResult.Success ? 200 : 400, deleteUserInfoResult.Message, deleteUserInfoResult.ContentType);
						break;
				}
				// tradings/{tradingdealid}
				break;
		}
	}
}