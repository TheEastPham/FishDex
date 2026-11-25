using System.Security.Claims;
using AquaHome.Common.Enums;
using AquaHome.Domain.DTOs;
using AquaHome.Domain.Services;
using AquaHome.Domain.Services.Interfaces;
using AquaHome.EFCore.Data;
using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository;
using FishLover.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace AquaHome.Tests;

/// <summary>
/// Chạy AquariumService trên repo thật + EF InMemory: bug "FishCount = 0" nằm ở câu query của repo
/// (thiếu Include Fish), mock repo sẽ không bắt được.
/// </summary>
public sealed class AquariumServiceTests : IDisposable
{
    private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Stranger = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private sealed class FakeUser(Guid id) : ICurrentUserSession
    {
        public Guid UserId => id;
        public string UserIdString => id.ToString();
        public string? UserName => "tester";
        public string? Email => null;
        public IEnumerable<Claim> Claims => [];
        public IReadOnlyList<string> Roles => [];
        public bool IsAdmin => false;
        public bool IsContentAdmin => false;
        public bool IsAuthenticated => true;
    }

    private sealed class NoQuota : IQuotaService
    {
        public Task<RoleQuotaDto> GetQuotaAsync(string role, CancellationToken ct = default) => throw new NotSupportedException();
        public Task EnforceCountLimitAsync(QuotaType quotaType, CancellationToken ct = default) => Task.CompletedTask;
        public Task ConsumeDailyAsync(QuotaType quotaType, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RoleQuotaDto>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<RoleQuotaDto?> UpdateAsync(string role, UpdateRoleQuotaRequest request, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private readonly AquaHomeDbContext _db = new(
        new DbContextOptionsBuilder<AquaHomeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public void Dispose()
    {
        _db.Dispose();
    }

    private AquariumService Service(Guid userId)
    {
        return new AquariumService(new AquariumRepository(_db), new FakeUser(userId), new NoQuota());
    }

    private async Task<Guid> SeedTankWithFishAsync()
    {
        var tank = new Aquarium { Id = Guid.NewGuid(), UserId = Owner, Name = "Bể medaka", CreatedAt = DateTime.UtcNow };
        _db.Aquariums.Add(tank);
        _db.AquariumFish.AddRange(
            new AquariumFish { AquariumId = tank.Id, SpecCode = 7, Quantity = 5, AddedAt = DateTime.UtcNow },
            new AquariumFish { AquariumId = tank.Id, SpecCode = 9, Quantity = 3, AddedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear(); // mô phỏng request mới: không còn entity nào đang tracked
        return tank.Id;
    }

    [Fact]
    public async Task GetById_reports_fish_count_and_total_quantity()
    {
        var id = await SeedTankWithFishAsync();

        var dto = await Service(Owner).GetByIdAsync(id);

        Assert.NotNull(dto);
        Assert.Equal(2, dto.FishCount);
        Assert.Equal(8, dto.TotalQuantity);
    }

    [Fact]
    public async Task Update_reports_fish_count_and_applies_the_change()
    {
        var id = await SeedTankWithFishAsync();

        var dto = await Service(Owner).UpdateAsync(id, new UpdateAquariumRequest("Tên mới", null, null, null, null, null, null, null));

        Assert.NotNull(dto);
        Assert.Equal("Tên mới", dto.Name);
        Assert.Equal(2, dto.FishCount);
        Assert.Equal(8, dto.TotalQuantity);
    }

    [Fact]
    public async Task Update_does_not_touch_the_fish_rows()
    {
        var id = await SeedTankWithFishAsync();

        await Service(Owner).UpdateAsync(id, new UpdateAquariumRequest("Tên mới", null, null, null, null, null, null, null));

        _db.ChangeTracker.Clear();
        Assert.Equal(2, await _db.AquariumFish.CountAsync(f => f.AquariumId == id));
        Assert.Equal(8, await _db.AquariumFish.Where(f => f.AquariumId == id).SumAsync(f => f.Quantity));
    }

    [Fact]
    public async Task Tank_without_fish_reports_zero()
    {
        var tank = new Aquarium { Id = Guid.NewGuid(), UserId = Owner, Name = "Bể trống", CreatedAt = DateTime.UtcNow };
        _db.Aquariums.Add(tank);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var dto = await Service(Owner).GetByIdAsync(tank.Id);

        Assert.Equal(0, dto!.FishCount);
        Assert.Equal(0, dto.TotalQuantity);
    }

    [Fact]
    public async Task Another_users_tank_is_not_found()
    {
        var id = await SeedTankWithFishAsync();

        Assert.Null(await Service(Stranger).GetByIdAsync(id));
        Assert.Null(await Service(Stranger).UpdateAsync(id, new UpdateAquariumRequest("x", null, null, null, null, null, null, null)));
    }
}
