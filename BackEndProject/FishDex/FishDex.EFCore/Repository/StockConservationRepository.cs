using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Stocks;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class StockConservationRepository(FishDexDbContext context) : GenericRepository<StockConservation>(context), IStockConservationRepository
{
    
}

