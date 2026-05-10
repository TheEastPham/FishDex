using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Ecosystem;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class EcosystemRepository(FishDexDbContext context) : GenericRepository<Ecosystem>(context), IEcosystemRepository
{
    
}

