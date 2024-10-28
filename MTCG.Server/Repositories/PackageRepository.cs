using MTCG.Server.Services;

namespace MTCG.Server.Repositories;

public class PackageRepository
{
	private readonly DatabaseConnection _dbConn = DatabaseConnection.Instance;
	private static NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
}