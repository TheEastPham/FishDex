const vi = {
  nav: {
    dashboard: 'Tổng quan',
    fishSearch: 'Tìm kiếm cá',
    aiChat: 'Chat AI',
    imageSearch: 'Tìm bằng hình',
    logout: 'Đăng xuất',
  },
  login: {
    subtitle: 'Quản lý bể cá của bạn',
    button: 'Đăng nhập',
  },
  fish: {
    title: 'Tìm kiếm cá',
    subtitle: 'Tìm kiếm trong ~8,883 loài cá aquarium từ TheFishLover',
    placeholder: 'Tên khoa học hoặc tên thường gọi...',
    allFamilies: 'Tất cả các họ',
    results: 'kết quả cho',
    error: 'Không thể tải kết quả. Kiểm tra FishDex Service.',
    emptyResult: 'Không tìm thấy loài nào cho',
    emptyState: 'Nhập tên loài để bắt đầu tìm kiếm',
    genus: 'Chi',
    detail: 'Chi tiết',
    viewProfile: 'Xem hồ sơ',
    viewProfileDetails: 'Xem chi tiết hồ sơ',
    viewFamily: 'Xem tất cả loài thuộc họ {{family}}',
    unknownFamily: 'Chưa rõ họ',
    share: 'Chia sẻ',
    addToFavorites: 'Thêm vào yêu thích',
    addToAquarium: '+ Thêm vào bể',
    addToAquariumDisabledTip: 'Cần AquaHome Service (Story 5.2)',
  },
  pagination: {
    prev: '← Trước',
    next: 'Tiếp →',
    page: 'Trang',
    of: '/',
  },
} as const;

export default vi;
