using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class SystemImageRepository(FishDexDbContext context) : GenericRepository<SystemImage>(context), ISystemImageRepository
{
    
}

