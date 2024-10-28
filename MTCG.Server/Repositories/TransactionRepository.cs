using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class TransactionRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();


}