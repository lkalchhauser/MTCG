using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;

namespace MTCG.Server.Repositories;

// Currently unused, but I'm leaving it in for possible further use
public class TransactionRepository(DatabaseConnection dbConn) : ITransactionRepository
{
	private readonly DatabaseConnection _dbConn = dbConn;
	private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
}