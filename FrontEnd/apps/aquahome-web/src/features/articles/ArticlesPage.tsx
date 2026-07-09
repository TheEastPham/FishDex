import { ExternalLink, Youtube, Facebook } from 'lucide-react';
import { useTranslation } from '@fishlover/shared';

// ─── Release notes data ───────────────────────────────────────────────────────
// Thêm version mới vào ĐẦU mảng (mới nhất hiển thị trên cùng).

interface FeatureGroup {
  group: string;
  items: string[];
}

interface ReleaseEntry {
  version: string;
  date: string;
  tag: 'new' | 'fix' | 'improve';
  summary: string;
  features: FeatureGroup[];
  roadmapTeaser?: string;
}

const RELEASES: ReleaseEntry[] = [
  {
    version: 'v1.0',
    date: '2026-06-18',
    tag: 'new',
    summary: 'Ra mắt FishLover — tra cứu loài cá khoa học và quản lý bể cá cá nhân.',
    features: [
      {
        group: '🔍 Tra cứu loài cá (FishDex)',
        items: [
          'Tìm kiếm theo tên khoa học hoặc tên thông dụng, hỗ trợ Tiếng Việt và English',
          'Trang chi tiết loài: taxonomy, điều kiện nước (nhiệt độ, pH, dH), sinh thái, bảo tồn IUCN / CITES, tuổi thọ',
          'Bản đồ phân bố địa lý theo quốc gia với toạ độ thực từ FishBase',
          'Ảnh loài (đực / cái / preferred) serve qua Cloudflare R2',
        ],
      },
      {
        group: '🐠 Quản lý bể cá (AquaHome)',
        items: [
          'Thêm / sửa / xóa bể cá, phân loại nước ngọt · mặn · lợ, kích thước và thể tích',
          'Theo dõi từng loài trong bể, xem phân bố địa lý toàn bộ cá trên 1 bản đồ',
          'Photo gallery: upload ảnh bể lên R2, nén ảnh phía client trước khi gửi',
          'Recently Viewed: lịch sử xem loài gần nhất, lưu trữ bằng Redis',
        ],
      },
    ],
    roadmapTeaser: 'v1.1 — Kênh cộng đồng đóng góp loài cá, nhắc nhở thay nước & châm phân, và sửa lỗi từ cộng đồng.',
  },
];

const COMMUNITY_LINKS = [
  {
    key: 'facebook' as const,
    icon: Facebook,
    url: 'https://www.facebook.com/profile.php?id=61580004981583',
    color: 'text-blue-400',
    bg: 'bg-blue-400/10 hover:bg-blue-400/20',
    border: 'border-blue-400/20',
  },
  {
    key: 'youtube' as const,
    icon: Youtube,
    url: 'https://www.youtube.com/@FishLoverOrg',
    color: 'text-red-400',
    bg: 'bg-red-400/10 hover:bg-red-400/20',
    border: 'border-red-400/20',
  },
];

// ─── Main page ────────────────────────────────────────────────────────────────

export default function ArticlesPage() {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-[#0F172A] text-white flex flex-col">
      {/* Header */}
      <div className="px-4 pt-6 pb-2 md:px-8">
        <h1 className="text-xl font-bold text-white">{t('articles.title')}</h1>
      </div>

      <div className="flex-1 px-4 md:px-8 py-4 space-y-8">

        {/* ── Release Notes ─────────────────────────────────── */}
        <section>
          <h2 className="text-sm font-semibold text-slate-400 uppercase tracking-wider mb-4">
            {t('articles.releaseNotes')}
          </h2>

          <div className="space-y-4">
            {RELEASES.map((release, idx) => (
              <div
                key={release.version}
                className="bg-slate-800/60 border border-slate-700/50 rounded-2xl p-5"
              >
                {/* Version header */}
                <div className="flex items-center gap-3 mb-2">
                  <span className="text-lg font-bold text-white">{release.version}</span>
                  {idx === 0 && (
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-sky-500/20 text-sky-300 border border-sky-500/30 font-medium">
                      {t('articles.latestBadge')}
                    </span>
                  )}
                  <span className="ml-auto text-xs text-slate-500">{release.date}</span>
                </div>

                {/* Summary */}
                <p className="text-sm text-slate-300 mb-4">{release.summary}</p>

                {/* Feature groups */}
                <div className="space-y-4">
                  {release.features.map((group) => (
                    <div key={group.group}>
                      <p className="text-sm font-semibold text-slate-200 mb-2">{group.group}</p>
                      <ul className="space-y-1.5 pl-1">
                        {group.items.map((item, i) => (
                          <li key={i} className="flex items-start gap-2 text-sm text-slate-400">
                            <span className="mt-1.5 w-1 h-1 rounded-full bg-slate-500 shrink-0" />
                            <span className="leading-snug">{item}</span>
                          </li>
                        ))}
                      </ul>
                    </div>
                  ))}
                </div>

                {/* Roadmap teaser */}
                {release.roadmapTeaser && (
                  <div className="mt-4 pt-4 border-t border-slate-700/50">
                    <p className="text-xs text-slate-500 italic">{release.roadmapTeaser}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>

        {/* ── Community links ────────────────────────────────── */}
        <section>
          <h2 className="text-sm font-semibold text-slate-400 uppercase tracking-wider mb-4">
            {t('articles.community')}
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
                  <p className="font-semibold text-white text-sm">
                    {t(`articles.${key}Page`)}
                  </p>
                  <p className="text-xs text-slate-400 mt-0.5 line-clamp-2">
                    {t(`articles.${key}Desc`)}
                  </p>
                </div>
                <ExternalLink className="w-4 h-4 shrink-0 text-slate-500" />
              </a>
            ))}
          </div>
        </section>

      </div>

      {/* Footer */}
      <footer className="px-4 py-6 text-center">
        <p className="text-xs text-slate-600">FishLover v1.0 · 2026</p>
      </footer>
    </div>
  );
}
