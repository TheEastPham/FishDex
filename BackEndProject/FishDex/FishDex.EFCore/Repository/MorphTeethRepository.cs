using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.MorphData;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class MorphTeethRepository(FishDexDbContext context) : GenericRepository<MorphTeeth>(context), IMorphTeethRepository
{
    
}

