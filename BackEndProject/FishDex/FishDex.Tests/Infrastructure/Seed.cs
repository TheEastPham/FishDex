using FishDex.EFCore.DbContexts;
using FishDex.EFCore.Entity.Cache;
using FishDex.EFCore.Entity.Ecologies;
using FishDex.EFCore.Entity.Media;
using FishDex.EFCore.Entity.Species;
using FishDex.EFCore.Entity.Stocks;
using FishLover.Shared.Common.Enum;

namespace FishDex.Tests.Infrastructure;

/// <summary>Dựng dữ liệu FishBase/community tối thiểu. Gọi <see cref="SaveAsync"/> khi xong.</summary>
public sealed class Seed(FishDexDbContext db)
{
    public Family Family(int famCode = 1, string name = "Adrianichthyidae")
    {
        var f = new Family { Id = Guid.NewGuid(), FamCode = famCode, Name = name, Order = "Beloniformes", Class = "Actinopteri" };
        db.Families.Add(f);
        return f;
    }

    public Genus Genus(Family family, int genusCode, string name = "Oryzias")
    {
        var g = new Genus { GenusCode = genusCode, FamId = family.Id, GenusName = name };
        db.Genuses.Add(g);
        return g;
    }

    public Species Species(Family family, int specCode, string name, Genus? genus = null,
        string? picNameMale = null, string? picNameFemale = null)
    {
        var s = new Species
        {
            Id                = Guid.NewGuid(),
            SpecCode          = specCode,
            FamId             = family.Id,
            FamCode           = family.FamCode,
            GenusCode         = genus?.GenusCode,
            SpeciesName       = name,
            WaterType         = WaterType.Freshwater,
            Length            = 4m,
            PicPreferredNameM = picNameMale,
            PicPreferredNameF = picNameFemale,
        };
        db.Species.Add(s);
        return s;
    }

    public CommonName Name(int specCode, string name, string language,
        bool isPreferred = false, int rank = 1, bool isVerified = true, Guid? contributedBy = null)
    {
        var n = new CommonName
        {
            SpecCode      = specCode,
            ComName       = name,
            Language      = language,
            IsPreferred   = isPreferred,
            Rank          = rank,
            IsVerified    = isVerified,
            ContributedBy = contributedBy,
        };
        db.CommonNames.Add(n);
        return n;
    }

    public SystemImage Image(int specCode, string name,
        bool preferred = false, bool male = false, bool female = false)
    {
        var i = new SystemImage
        {
            Id               = Guid.NewGuid(),
            SpecCode         = specCode,
            Name             = name,
            PictureType      = "adult",
            PicPreferred     = preferred ? true : null,
            PicPreferredMale = male ? true : null,
            PicPreferredFem  = female ? true : null,
        };
        db.SystemImages.Add(i);
        return i;
    }

    public Ecology Ecology(int specCode, int ecologyId, int autoctr)
    {
        var e = new Ecology { EcologyId = ecologyId, autoctr = autoctr, SpecCode = specCode };
        db.Ecologies.Add(e);
        return e;
    }

    public void Feeding(int ecologyId, string feedingType, decimal? dietTroph = null, decimal? foodTroph = null)
    {
        db.FeedingAndDiets.Add(new FeedingAndDiet
        {
            EcologyId = ecologyId, FeedingType = feedingType, DietTroph = dietTroph, FoodTroph = foodTroph,
        });
    }

    public void Associations(int ecologyId, bool schooling = false, bool shoaling = false, bool solitary = false)
    {
        db.Associations.Add(new Associations
        {
            EcologyId = ecologyId, Schooling = schooling, Shoaling = shoaling, Solitary = solitary,
        });
    }

    public void HabitatZone(int ecologyId, bool stream = false, bool lakes = false)
    {
        db.HabitatZones.Add(new HabitatZone { EcologyId = ecologyId, Stream = stream, Lakes = lakes });
    }

    public void Substrate(int ecologyId, bool sand = false, bool gravel = false, bool endofauna = false)
    {
        db.Substrates.Add(new Substrate { EcologyId = ecologyId, Sand = sand, Gravel = gravel, Endofauna = endofauna });
    }

    public void SpecialHabitat(int ecologyId, bool driftwood = false, bool macrophyte = false)
    {
        db.SpecialHabitats.Add(new SpecialHabitat { EcologyId = ecologyId, Driftwood = driftwood, Macrophyte = macrophyte });
    }

    /// <summary>Dòng cache FishBase kiểu cũ (trước migration DropFishBaseSpeciesSnapshots) — dữ liệu lỗi thời.</summary>
    public void StaleFishBaseSnapshot(int specCode, string name, string? commonName, string? thumbnailKey)
    {
        db.SpeciesSnapshots.Add(new SpeciesSnapshot
        {
            SpecCode           = specCode,
            DataSource         = SnapshotDataSource.FishBase,
            SpeciesName        = name,
            CommonName         = commonName,
            ThumbnailObjectKey = thumbnailKey,
            PopulatedAt        = DateTime.UtcNow,
            PopulatedFrom      = SnapshotPopulatedFrom.FishBase,
        });
    }

    public void Stock(int specCode, int stockCode, double? tempMin = null, string? iucn = null)
    {
        db.Stocks.Add(new Stock { StockCode = stockCode, SpecCode = specCode });
        db.StockEnvironments.Add(new StockEnvironment { StockCode = stockCode, TempMin = tempMin });
        if (iucn != null)
            db.StockConservations.Add(new StockConservation { StockCode = stockCode, IUCN_Code = iucn });
    }

    public SpeciesSnapshot Community(int specCode, string name, bool verified = true,
        string? commonName = null, string? thumbnailKey = null)
    {
        var s = new SpeciesSnapshot
        {
            SpecCode           = specCode,
            DataSource         = SnapshotDataSource.Community,
            IsVerified         = verified,
            SpeciesName        = name,
            CommonName         = commonName,
            FamilyName         = "Adrianichthyidae",
            WaterType          = WaterType.Freshwater,
            Length             = 3.5m,
            ThumbnailObjectKey = thumbnailKey,
            PopulatedAt        = DateTime.UtcNow,
            PopulatedFrom      = SnapshotPopulatedFrom.Manual,
        };
        db.SpeciesSnapshots.Add(s);
        return s;
    }

    public Task SaveAsync()
    {
        return db.SaveChangesAsync();
    }
}
