using MTCG.Server.Models;

namespace MTCG.Server.Repositories.Interfaces;

public interface IPackageRepository
{
	public bool AddPackage(Package package);
	public Package? GetPackageIdByName(string name);
	public bool UpdatePackage(Package package);
	public bool AddPackageCardRelation(int packageId, int cardId);
	public int GetRandomPackageId();
	public Package GetPackageWithoutCardsById(int id);
	public List<int> GetPackageCardIds(int packageId);
	public bool DeletePackage(int id);
}