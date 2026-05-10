using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Occurrence;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class OccurrenceRepository(FishDexDbContext context) : GenericRepository<Occurrence>(context), IOccurrenceRepository
{
    
}

