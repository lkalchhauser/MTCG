using MTCG.Server.HTTP;
using MTCG.Server.Util.HelperClasses;

namespace MTCG.Server.Services.Interfaces;

public interface ITransactionService
{
	public Result GetRandomPackageForUser(IHandler handler);
	public void RemoveOnePackageById(int packageId);
}