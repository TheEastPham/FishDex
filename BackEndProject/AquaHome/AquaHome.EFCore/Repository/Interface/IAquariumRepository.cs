using AquaHome.EFCore.Entity;
using AquaHome.EFCore.Repository.Base;

namespace AquaHome.EFCore.Repository.Interface;

public interface IAquariumRepository : IGenericRepository<Aquarium>
{
    Task<IReadOnlyList<Aquarium>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Aquarium?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken ct = default);

    Task<AquariumFish?> GetFishEntryAsync(Guid aquariumId, int specCode, CancellationToken ct = default);
    Task AddFishAsync(AquariumFish fish, CancellationToken ct = default);
    Task RemoveFishAsync(AquariumFish fish, CancellationToken ct = default);

    Task<IReadOnlyList<AquariumFish>> GetFishListAsync(Guid aquariumId, CancellationToken ct = default);

    /// <summary>
    /// Gom các loài đang có trong bể, nhóm theo quốc gia của bể. Nguồn dữ liệu cho lớp market
    /// bên FishDex: bể ở quốc gia X có cá Z nghĩa là quốc gia X bán cá Z.
    ///
    /// <para><b>Không trả về bất kỳ thông tin user hay bể nào</b> — chỉ cặp quốc gia và mã loài.
    /// Quan hệ chỉ đi một chiều, không được truy ngược từ danh sách market về chủ bể.</para>
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<int>>> GetSpecCodesByCountryAsync(
        CancellationToken ct = default);
}
