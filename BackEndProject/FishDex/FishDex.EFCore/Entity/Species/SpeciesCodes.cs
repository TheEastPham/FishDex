namespace FishDex.EFCore.Entity.Species;

/// <summary>
/// Không gian SpecCode chia hai nửa: dưới <see cref="CommunityStart"/> là loài FishBase (bảng
/// <c>Species</c> và các bảng FishBase đi kèm), từ <see cref="CommunityStart"/> trở lên là loài
/// cộng đồng — chỉ tồn tại trong <c>SpeciesSnapshots</c> với <c>DataSource = Community</c>.
/// </summary>
public static class SpeciesCodes
{
    public const int CommunityStart = 500_000;

    public static bool IsCommunity(int specCode)
    {
        return specCode >= CommunityStart;
    }
}
