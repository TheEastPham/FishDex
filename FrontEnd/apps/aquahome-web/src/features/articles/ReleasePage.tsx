import { useState } from 'react';
import { ExternalLink, Youtube, Facebook, Clock, CalendarDays, ChevronDown } from 'lucide-react';
import { useTranslation } from '@fishlover/shared';

// ─── Data ─────────────────────────────────────────────────────────────────────

type Lang = { vi: string; en: string };

interface FeatureGroup {
  group: Lang;
  items: Lang[];
}

type ReleaseStatus = 'released' | 'upcoming' | 'planning';

interface ReleaseEntry {
  version: string;
  status: ReleaseStatus;
  summary: Lang;
  features: FeatureGroup[];
}

const RELEASES: ReleaseEntry[] = [
  {
    version: 'v1.0',
    status: 'released',
    summary: {
      vi: 'Ra mắt FishLover — tra cứu loài cá khoa học và quản lý bể cá cá nhân.',
      en: 'FishLover launches — scientific fish lookup and personal aquarium management.',
    },
    features: [
      {
        group: { vi: '🔍 Tra cứu loài cá (FishDex)', en: '🔍 Fish Encyclopedia (FishDex)' },
        items: [
          { vi: 'Tìm kiếm theo tên khoa học hoặc tên thông dụng, hỗ trợ Tiếng Việt và English', en: 'Search by scientific or common name, supports Vietnamese and English' },
          { vi: 'Trang chi tiết loài: taxonomy, điều kiện nước (nhiệt độ, pH, dH), sinh thái, bảo tồn IUCN / CITES, tuổi thọ', en: 'Species detail page: taxonomy, water parameters (temp, pH, dH), ecology, IUCN / CITES conservation, lifespan' },
          { vi: 'Bản đồ phân bố địa lý theo quốc gia với toạ độ thực từ FishBase', en: 'Distribution map by country with real coordinates from FishBase' },
          { vi: 'Ảnh loài (đực / cái / preferred) serve qua Cloudflare R2', en: 'Species photos (male / female / preferred) served via Cloudflare R2' },
        ],
      },
      {
        group: { vi: '🐠 Quản lý bể cá (AquaHome)', en: '🐠 Aquarium Manager (AquaHome)' },
        items: [
          { vi: 'Thêm / sửa / xóa bể cá, phân loại nước ngọt · mặn · lợ, kích thước và thể tích', en: 'Add / edit / delete tanks, classify freshwater · saltwater · brackish, size and volume' },
          { vi: 'Theo dõi từng loài trong bể, xem phân bố địa lý toàn bộ cá trên 1 bản đồ', en: 'Track each species per tank, view all fish distribution on a single map' },
          { vi: 'Photo gallery: upload ảnh bể lên R2, nén ảnh phía client trước khi gửi', en: 'Photo gallery: upload tank photos to R2 with client-side compression before sending' },
          { vi: 'Recently Viewed: lịch sử xem loài gần nhất, lưu trữ bằng Redis', en: 'Recently Viewed: latest species history, stored in Redis' },
        ],
      },
    ],
  },
  {
    version: 'v1.1',
    status: 'upcoming',
    summary: {
      vi: 'Mở rộng cộng đồng — để người nuôi cá cùng nhau xây dựng cơ sở dữ liệu, và giúp bể cá luôn ở trạng thái tốt nhất.',
      en: 'Community expansion — let fish keepers build the database together, and keep every tank in top shape.',
    },
    features: [
      {
        group: { vi: '🌿 Cộng đồng đóng góp loài', en: '🌿 Community Species Contributions' },
        items: [
          { vi: 'Người dùng có thể đề xuất thêm loài cá chưa có trong FishDex — ảnh, mô tả, tên thông dụng', en: 'Users can suggest species not yet in FishDex — photos, description, common names' },
          { vi: 'Hệ thống duyệt bài: đội ngũ kiểm duyệt xem xét trước khi publish', en: 'Moderation system: review team approves before publishing' },
          { vi: 'Ghi nhận người đóng góp trên trang chi tiết loài', en: 'Contributors credited on each species detail page' },
        ],
      },
      {
        group: { vi: '🔔 Nhắc nhở chăm sóc bể', en: '🔔 Tank Care Reminders' },
        items: [
          { vi: 'Lịch thay nước định kỳ: cài tần suất theo từng bể, nhận thông báo đúng hẹn', en: 'Water change schedule: set frequency per tank, get notified on time' },
          { vi: 'Nhắc bón phân cho bể cây thuỷ sinh theo chu kỳ tự đặt', en: 'Fertiliser reminders for planted tanks on a custom cycle' },
          { vi: 'Lịch sử chăm sóc: xem lại lần cuối thay nước / châm phân là khi nào', en: 'Care history: see when you last changed water or added fertiliser' },
        ],
      },
      {
        group: { vi: '🐛 Sửa lỗi từ cộng đồng', en: '🐛 Community Bug Fixes' },
        items: [
          { vi: 'Xử lý các lỗi và phản hồi thu thập được sau đợt ra mắt v1.0', en: 'Addressing bugs and feedback collected after the v1.0 launch' },
        ],
      },
    ],
  },
  {
    version: 'v2.0',
    status: 'planning',
    summary: {
      vi: 'Đưa AI vào trái tim của FishLover — từ nhận diện loài qua ảnh đến tư vấn thông minh cho từng bể cá.',
      en: 'Bringing AI to the heart of FishLover — from photo species recognition to smart per-tank advice.',
    },
    features: [
      {
        group: { vi: '📷 Tìm kiếm bằng hình ảnh', en: '📷 Image-Based Search' },
        items: [
          { vi: 'Chụp hoặc upload ảnh cá — AI nhận diện loài và trả về trang chi tiết tương ứng', en: 'Snap or upload a fish photo — AI identifies the species and links to its detail page' },
          { vi: 'Hỗ trợ nhiều góc chụp, cả cá trong bể lẫn ngoài tự nhiên', en: 'Works from multiple angles, both tank fish and wild specimens' },
        ],
      },
      {
        group: { vi: '🤖 Trợ lý AI chuyên về cá', en: '🤖 Fish-Specialist AI Assistant' },
        items: [
          { vi: 'Hỏi tự do về tập tính loài, chế độ ăn, điều kiện nước — AI trả lời có trích dẫn dữ liệu từ FishDex', en: 'Ask anything about species behaviour, diet, water conditions — AI answers with FishDex citations' },
          { vi: 'Kiểm tra độ tương thích: "Cá này nuôi chung với cá kia có ổn không?" — phân tích theo lãnh thổ, pH, nhiệt độ, tập tính', en: 'Compatibility check: "Can these two fish live together?" — analysis by territory, pH, temperature, behaviour' },
          { vi: 'Tư vấn bể sinh sản vs bể cộng đồng: khác nhau về setup, mật độ, cây, góc trú ẩn', en: 'Breeding tank vs community tank advice: differences in setup, stocking density, plants, hiding spots' },
        ],
      },
      {
        group: { vi: '🔬 Dữ liệu thông minh hơn', en: '🔬 Smarter Data' },
        items: [
          { vi: 'Gợi ý loài phù hợp dựa trên thông số nước hiện tại của bể', en: 'Species suggestions based on your tank\'s current water parameters' },
          { vi: 'Cảnh báo khi thêm loài có nguy cơ xung đột với cá đang nuôi', en: 'Warning when adding a species likely to conflict with existing fish' },
        ],
      },
    ],
  },
];

const COMMUNITY_LINKS = [
  {
    key: 'facebook' as const,
    icon: Facebook,
    url: 'https://www.facebook.com/profile.php?id=61580004981583',
    color: 'text-blue-400',
    bg: 'bg-blue-500/10 hover:bg-blue-500/15',
    border: 'border-blue-500/20',
  },
  {
    key: 'youtube' as const,
    icon: Youtube,
    url: 'https://www.youtube.com/@FishLoverOrg',
    color: 'text-red-400',
    bg: 'bg-red-500/10 hover:bg-red-500/15',
    border: 'border-red-500/20',
  },
];

// ─── Badge config ──────────────────────────────────────────────────────────────

const STATUS_CONFIG: Record<ReleaseStatus, {
  badge: string;
  badgeClass: string;
  cardClass: string;
  textClass: string;
  Icon?: React.ComponentType<{ className?: string }>;
}> = {
  released: {
    badge: '',
    badgeClass: 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30',
    cardClass: 'bg-slate-800/60 border-slate-700/50',
    textClass: 'text-slate-300',
    Icon: undefined,
  },
  upcoming: {
    badge: 'release.badgeUpcoming',
    badgeClass: 'bg-amber-500/15 text-amber-300 border-amber-500/25',
    cardClass: 'bg-slate-800/30 border-slate-700/30 border-dashed',
    textClass: 'text-slate-500',
    Icon: Clock,
  },
  planning: {
    badge: 'release.badgePlanning',
    badgeClass: 'bg-violet-500/15 text-violet-300 border-violet-500/25',
    cardClass: 'bg-slate-800/20 border-slate-700/20 border-dashed',
    textClass: 'text-slate-600',
    Icon: CalendarDays,
  },
};

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ReleasePage() {
  const { t, i18n } = useTranslation();
  const lang = i18n.language?.startsWith('en') ? 'en' : 'vi';

  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  function toggle(version: string) {
    setExpanded(prev => ({ ...prev, [version]: !prev[version] }));
  }

  return (
    <div className="min-h-screen bg-[#0F172A] text-white flex flex-col">

      {/* Hero */}
      <div className="px-4 pt-8 pb-6 md:px-8 md:pt-10">
        <p className="text-xs font-semibold text-sky-400 uppercase tracking-widest mb-1">FishLover</p>
        <h1 className="text-2xl font-bold text-white leading-snug">{t('release.title')}</h1>
        <p className="mt-1.5 text-sm text-slate-400 max-w-prose">{t('release.subtitle')}</p>
      </div>

      <div className="flex-1 px-4 md:px-8 pb-6 space-y-10">

        {/* ── Release Notes ──────────────────────────────────── */}
        <section>
          <h2 className="text-xs font-semibold text-slate-500 uppercase tracking-widest mb-4">
            {t('release.sectionNotes')}
          </h2>

          <div className="space-y-4">
            {RELEASES.map((release, idx) => {
              const cfg = STATUS_CONFIG[release.status];
              const isReleased = release.status === 'released';
              const isOpen = isReleased || expanded[release.version];

              return (
                <div key={release.version} className={`rounded-2xl border ${cfg.cardClass} overflow-hidden`}>

                  {/* Header row — always visible */}
                  <div
                    className={`flex items-center gap-2.5 px-5 py-4 ${!isReleased ? 'cursor-pointer select-none' : ''}`}
                    onClick={() => !isReleased && toggle(release.version)}
                  >
                    <span className={`text-lg font-bold ${isReleased ? 'text-white' : 'text-slate-400'}`}>
                      {release.version}
                    </span>

                    {/* Live badge */}
                    {isReleased && idx === 0 && (
                      <span className={`text-[10px] px-2 py-0.5 rounded-full border font-semibold ${cfg.badgeClass}`}>
                        {t('release.badgeLive')}
                      </span>
                    )}

                    {/* Upcoming / Planning badge */}
                    {!isReleased && cfg.Icon && (
                      <span className={`flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full border font-medium ${cfg.badgeClass}`}>
                        <cfg.Icon className="w-2.5 h-2.5" />
                        {t(cfg.badge)}
                      </span>
                    )}

                    {/* Summary (collapsed preview) */}
                    {!isReleased && !isOpen && (
                      <p className={`ml-1 text-xs truncate flex-1 ${cfg.textClass}`}>
                        {release.summary[lang]}
                      </p>
                    )}

                    {/* Chevron */}
                    {!isReleased && (
                      <ChevronDown
                        className={`w-4 h-4 ml-auto shrink-0 text-slate-500 transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`}
                      />
                    )}
                  </div>

                  {/* Body — expandable */}
                  {isOpen && (
                    <div className="px-5 pb-5">
                      {/* Summary */}
                      <p className={`text-sm mb-4 leading-relaxed ${cfg.textClass}`}>
                        {release.summary[lang]}
                      </p>

                      {/* Feature groups */}
                      <div className="space-y-4">
                        {release.features.map((group, gi) => (
                          <div key={gi}>
                            <p className={`text-sm font-semibold mb-2 ${isReleased ? 'text-slate-200' : 'text-slate-400'}`}>
                              {group.group[lang]}
                            </p>
                            <ul className="space-y-2 pl-1">
                              {group.items.map((item, i) => (
                                <li key={i} className="flex items-start gap-2.5 text-sm">
                                  <span className={`mt-2 w-1 h-1 rounded-full shrink-0 ${isReleased ? 'bg-slate-500' : 'bg-slate-600'}`} />
                                  <span className={`leading-relaxed ${isReleased ? 'text-slate-400' : 'text-slate-500'}`}>
                                    {item[lang]}
                                  </span>
                                </li>
                              ))}
                            </ul>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                </div>
              );
            })}
          </div>
        </section>

        {/* ── Community ──────────────────────────────────────── */}
        <section>
          <h2 className="text-xs font-semibold text-slate-500 uppercase tracking-widest mb-4">
            {t('release.sectionCommunity')}
          </h2>

          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {COMMUNITY_LINKS.map(({ key, icon: Icon, url, color, bg, border }) => (
              <a
                key={key}
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className={`flex items-center gap-3 p-4 rounded-2xl border ${bg} ${border} transition-colors`}
              >
                <Icon className={`w-6 h-6 shrink-0 ${color}`} />
                <div className="min-w-0 flex-1">
                  <p className="font-semibold text-white text-sm">{t(`release.${key}Page`)}</p>
                  <p className="text-xs text-slate-400 mt-0.5">{t(`release.${key}Desc`)}</p>
                </div>
                <ExternalLink className="w-4 h-4 shrink-0 text-slate-500" />
              </a>
            ))}
          </div>
        </section>

      </div>

      {/* Footer */}
      <footer className="px-4 py-8 border-t border-slate-800">
        <div className="max-w-screen-md mx-auto flex flex-col items-center gap-2 text-center">
          <p className="text-sm font-semibold text-slate-300 tracking-wide">🐟 FishLover</p>
          <p className="text-xs text-slate-500">{t('release.footerTagline')}</p>
          <p className="text-xs text-slate-600 mt-1">v1.0 · 2026 · Made with ❤️ for the aquarium community</p>
        </div>
      </footer>

    </div>
  );
}
