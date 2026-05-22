using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class CommonNameRepository(FishDexDbContext context)
    : GenericRepository<CommonName>(context), ICommonNameRepository
{
}
