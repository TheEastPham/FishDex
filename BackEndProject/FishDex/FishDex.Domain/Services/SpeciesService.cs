using AutoMapper;
using FishDex.Domain.DTOs.Common;
using FishDex.Domain.DTOs.Species;
using FishDex.Domain.Services.Interfaces;
using FishDex.EFCore.Repository.Interface;

namespace FishDex.Domain.Services;

public class SpeciesService(
    ISpeciesRepository speciesRepo,
    IFamiliesRepository familyRepo,
    IGenusRepository genusRepo,
    IMapper mapper) : ISpeciesService
{
    public async Task<PagedResult<SpeciesDto>> GetSpeciesAsync(GetSpeciesRequest request, CancellationToken ct = default)
    {
        var all = await speciesRepo.FindAsync(s =>
            (request.GenusCode == null || s.GenusCode == request.GenusCode) &&
            (request.SearchTerm == null || s.SpeciesName.Contains(request.SearchTerm)));

        var list = all.ToList();
        var items = list
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<SpeciesDto>
        {
            Items = mapper.Map<List<SpeciesDto>>(items),
            TotalCount = list.Count,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<SpeciesDto?> GetBySpecCodeAsync(int specCode, CancellationToken ct = default)
    {
        var results = await speciesRepo.FindAsync(s => s.SpecCode == specCode);
        var entity = results.FirstOrDefault();
        return entity is null ? null : mapper.Map<SpeciesDto>(entity);
    }

    public async Task<IReadOnlyList<FamilyDto>> GetFamiliesAsync(CancellationToken ct = default)
    {
        var families = await familyRepo.FindAsync(_ => true);
        return mapper.Map<List<FamilyDto>>(families.ToList());
    }

    public async Task<IReadOnlyList<GenusDto>> GetGenusByFamilyAsync(Guid famId, CancellationToken ct = default)
    {
        var genera = await genusRepo.FindAsync(g => g.FamId == famId);
        return mapper.Map<List<GenusDto>>(genera.ToList());
    }
}
