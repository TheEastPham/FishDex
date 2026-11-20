import { Link } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useTranslation } from '@fishlover/shared';
import { PRIVACY, TERMS, type Lang, type LegalDoc } from './legalContent';

interface Props {
  doc: 'privacy' | 'terms';
}

/**
 * Trang pháp lý công khai. KHÔNG nằm trong AppShell và KHÔNG qua AuthGuard — Google phải
 * đọc được hai URL này khi duyệt OAuth consent screen, nên chúng phải mở cho cả khách
 * chưa đăng nhập lẫn bot.
 *
 * Ngôn ngữ: `vi` khi UI đang là tiếng Việt, còn lại dùng `en`. Người dùng chọn Deutsch
 * hay 中文 sẽ thấy bản tiếng Anh — thà đọc được tiếng Anh hơn là gặp tiếng Việt không hiểu.
 */
export default function LegalPage({ doc }: Props) {
  const { i18n } = useTranslation();
  const lang: keyof Lang = i18n.language?.startsWith('vi') ? 'vi' : 'en';
  const data: LegalDoc = doc === 'privacy' ? PRIVACY : TERMS;
  const other = doc === 'privacy'
    ? { to: '/terms', label: { vi: 'Điều khoản sử dụng', en: 'Terms of Service' } }
    : { to: '/privacy', label: { vi: 'Chính sách quyền riêng tư', en: 'Privacy Policy' } };

  return (
    <div className="min-h-screen bg-[#0F172A] text-white">
      <div className="mx-auto max-w-3xl px-5 py-8 md:px-8 md:py-12">

        <Link
          to="/"
          className="inline-flex items-center gap-1.5 text-sm text-slate-400 hover:text-white transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          {lang === 'vi' ? 'Về trang chủ' : 'Back to home'}
        </Link>

        <header className="mt-6">
          <p className="text-xs font-semibold text-sky-400 uppercase tracking-widest mb-1">
            FishLover
          </p>
          <h1 className="text-2xl md:text-3xl font-bold leading-snug">{data.title[lang]}</h1>
          <p className="mt-2 text-xs text-slate-500">
            {lang === 'vi' ? 'Cập nhật lần cuối' : 'Last updated'}: {data.updated}
          </p>
          <p className="mt-4 text-sm text-slate-300 leading-relaxed">{data.intro[lang]}</p>
        </header>

        <div className="mt-10 space-y-9">
          {data.sections.map((s) => (
            <section key={s.heading.en}>
              <h2 className="text-base font-semibold text-white mb-3">{s.heading[lang]}</h2>

              {s.paragraphs?.map((p) => (
                <p key={p.en} className="text-sm text-slate-300 leading-relaxed mb-3">
                  {p[lang]}
                </p>
              ))}

              {s.bullets && (
                <ul className="mt-1 space-y-2">
                  {s.bullets.map((b) => (
                    <li key={b.en} className="flex gap-2.5 text-sm text-slate-300 leading-relaxed">
                      <span className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-sky-500" />
                      <span>{b[lang]}</span>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          ))}
        </div>

        <footer className="mt-12 border-t border-slate-800 pt-6 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm">
          <Link to={other.to} className="text-sky-400 hover:text-sky-300 transition-colors">
            {other.label[lang]}
          </Link>
          <a
            href="mailto:admin@fishlover.org"
            className="text-slate-400 hover:text-white transition-colors"
          >
            admin@fishlover.org
          </a>
        </footer>

      </div>
    </div>
  );
}
