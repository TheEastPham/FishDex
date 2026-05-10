using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class SubstrateRepository(FishDexDbContext context) : GenericRepository<Substrate>(context), ISubstrateRepository
{
    
}

