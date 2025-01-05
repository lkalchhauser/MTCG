using MTCG.Server.Repositories.Interfaces;
using MTCG.Server.Util;

namespace MTCG.Server.Repositories;

public class TransactionRepository : ITransactionRepository
{
	private readonly DatabaseConnection _dbConn;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
	public TransactionRepository(DatabaseConnection dbConn)
	{
		_dbConn = dbConn;
	}
}