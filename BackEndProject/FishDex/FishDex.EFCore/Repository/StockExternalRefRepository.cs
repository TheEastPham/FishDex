using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Stocks;
using FishDex.EFCore.Repository.BaseGeneric;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.EFCore.Repository;

public class StockExternalRefRepository(FishDexDbContext context) : GenericRepository<StockExternalRef>(context), IStockExternalRefRepository
{
    
}

