using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MTCG.Server.Models;
using MTCG.Server.Services;
using MTCG.Server.Services.Interfaces;
using MTCG.Server.Util;
using MTCG.Server.Util.Enums;

namespace MTCG.Server.HTTP;

public class Router
{
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	private readonly IUserService _userService;
	private readonly ICardService _cardService;
	private readonly ITransactionService _transactionService;
	private readonly IDeckService _deckService;
	private readonly IBattleService _battleService;
	private readonly ITradeService _tradingService;
	private readonly IHelperService _helperService;

	public Router(IUserService userService, ICardService cardService, ITransactionService transactionService, IDeckService deckService, IBattleService battleService, ITradeService tradingService, IHelperService helperService)
	{
		_userService = userService;
		_cardService = cardService;
		_transactionService = transactionService;
		_deckService = deckService;
		_battleService = battleService;
		_tradingService = tradingService;
		_helperService = helperService;
	}

	public async void HandleIncoming(IHandler handler)
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
						if (!_userService.IsUserAuthorized(handler) || !_helperService.IsRequestedUserAuthorizedUser(handler))
						{
							handler.Reply(401);
							break;
						}

						var getUserInfoResult = _userService.GetUserInformationForUser(handler);
						handler.Reply(getUserInfoResult.StatusCode, getUserInfoResult.Message, getUserInfoResult.ContentType);
						break;
					case "/cards":
						_logger.Debug("Routing GET /cards");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var getCardsResult = _cardService.ShowAllCardsForUser(handler);
						handler.Reply(getCardsResult.StatusCode, getCardsResult.Message, getCardsResult.ContentType);
						// return all cards
						break;
					case { } s when s.StartsWith("/deck"):
						_logger.Debug("Routing GET /deck");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var userDeckResult = _deckService.GetDeckForCurrentUser(handler);
						handler.Reply(userDeckResult.StatusCode, userDeckResult.Message, userDeckResult.ContentType);
						break;
					case "/stats":
						_logger.Debug("Routing GET /stats");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var userStatsResult = _userService.GetUserStats(handler);
						handler.Reply(userStatsResult.StatusCode, userStatsResult.Message, userStatsResult.ContentType);
						break;
					case "/scoreboard":
						_logger.Debug("Routing GET /scoreboard");
						var getScoreboardResult = _userService.GetScoreboard(handler);
						handler.Reply(getScoreboardResult.StatusCode, getScoreboardResult.Message, getScoreboardResult.ContentType);
						break;
					case "/tradings":
						_logger.Debug("Routing GET /tradings");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}

						var getAllTradesResult = _tradingService.GetCurrentlyActiveTrades(handler);
						handler.Reply(getAllTradesResult.StatusCode, getAllTradesResult.Message, getAllTradesResult.ContentType);
						break;
					default:
						handler.Reply(404);
						break;
				}
				break;
			case "POST":
				switch (handler.Path)
				{
					case "/users":
						_logger.Debug("Routing POST /user");
						var userRegisterResult = _userService.RegisterUser(handler);
						handler.Reply(userRegisterResult.StatusCode, userRegisterResult.Message, userRegisterResult.ContentType);
						break;
					case "/sessions":
						_logger.Debug("Routing POST /sessions");
						var userLoginResult = _userService.LoginUser(handler);
						handler.Reply(userLoginResult.StatusCode, userLoginResult.Message, userLoginResult.ContentType);
						break;
					case "/packages":
						_logger.Debug("Routing POST /packages");
						var authUser = _userService.GetAuthorizedUserWithToken(handler.GetAuthorizationToken());
						if (authUser is not { Username: "admin" })
						{
							handler.Reply(403);
							return;
						}
						handler.AuthorizedUser = authUser;
						var createPackageResult = _cardService.CreatePackageAndCards(handler);
						handler.Reply(createPackageResult.StatusCode, createPackageResult.Message, createPackageResult.ContentType);
						break;
					case "/transactions/packages":
						_logger.Debug("Routing POST /transactions/packages");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var getPackageResult = _transactionService.GetRandomPackageForUser(handler);
						handler.Reply(getPackageResult.StatusCode, getPackageResult.Message, getPackageResult.ContentType);
						break;
					case { } s when s.StartsWith("/battles"):
						_logger.Debug("Routing POST /battles");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}

						var battleRequestResult = await _battleService.WaitForBattleAsync(handler, TimeSpan.FromMinutes(1), _deckService, _cardService);
						handler.Reply(battleRequestResult.StatusCode, battleRequestResult.Message, battleRequestResult.ContentType);
						break;
					case { } s when s.StartsWith("/tradings/"):
						_logger.Debug("Routing POST /tradings/");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var acceptTradingOffer = _tradingService.AcceptTradeOffer(handler);
						handler.Reply(acceptTradingOffer.StatusCode, acceptTradingOffer.Message, acceptTradingOffer.ContentType);
						break;
					case "/tradings":
						_logger.Debug("Routing POST /tradings");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var createTradingOffer = _tradingService.CreateTradeOffer(handler);
						handler.Reply(createTradingOffer.StatusCode, createTradingOffer.Message, createTradingOffer.ContentType);
						break;
					default:
						handler.Reply(404);
						break;
				}
				break;
			case "PUT":
				switch (handler.Path)
				{
					case "/deck":
						_logger.Debug("Routing PUT /deck");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}

						var setDeckResult = _deckService.SetDeckForCurrentUser(handler);
						handler.Reply(setDeckResult.StatusCode, setDeckResult.Message, setDeckResult.ContentType);
						break;
					case { } s when s.StartsWith("/users/password"):
						_logger.Debug($"Routing PUT {handler.Path}");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var updatePasswordResult = _userService.UpdatePassword(handler);
						handler.Reply(updatePasswordResult.StatusCode, updatePasswordResult.Message, updatePasswordResult.ContentType);
						break;
					case { } s when s.StartsWith("/users/"):
						_logger.Debug($"Routing PUT {handler.Path}");
						if (!_userService.IsUserAuthorized(handler) || !_helperService.IsRequestedUserAuthorizedUser(handler))
						{
							handler.Reply(401);
							break;
						}

						var addOrUpdateUserInfoResult = _userService.AddOrUpdateUserInfo(handler);
						handler.Reply(addOrUpdateUserInfoResult.StatusCode, addOrUpdateUserInfoResult.Message, addOrUpdateUserInfoResult.ContentType);
						break;
				}
				break;
			case "DELETE":
				switch (handler.Path)
				{
					case "/users/userinfo":
						_logger.Debug("Routing DELETE /users/userinfo");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var deleteUserInfoResult = _userService.DeleteUserInfo(handler);
						handler.Reply(deleteUserInfoResult.StatusCode, deleteUserInfoResult.Message, deleteUserInfoResult.ContentType);
						break;
					case { } s when s.StartsWith("/tradings/"):
						_logger.Debug("Routing DELETE /tradings/");
						if (!_userService.IsUserAuthorized(handler))
						{
							handler.Reply(401);
							break;
						}
						var deleteTradeResult = _tradingService.DeleteTrade(handler);
						handler.Reply(deleteTradeResult.StatusCode, deleteTradeResult.Message, deleteTradeResult.ContentType);
						break;
				}
				break;
		}
	}
}