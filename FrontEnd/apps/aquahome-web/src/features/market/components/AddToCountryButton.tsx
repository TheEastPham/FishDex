import { useState, useEffect } from 'react';
import {
  cn, useTranslation, useAuthStore,
  addTradedSpecies, removeTradedSpecies, getSellingCountries,
  getStoredCountry,
} from '@fishlover/shared';
import { Store, Check, Loader2 } from 'lucide-react';

interface Props {
  specCode: number;
  className?: string;
  /** `icon` cho thẻ trong lưới (chỗ chật), `full` cho trang chi tiết loài. */
  variant?: 'icon' | 'full';
}

const ADMIN_ROLES = ['SystemAdmin', 'ContentAdmin'];

/**
 * Thêm/gỡ một loài khỏi danh sách market của quốc gia đang chọn.
 *
 * **Chỉ hiện với SystemAdmin và ContentAdmin.** Người dùng thường không có nút nào — danh sách
 * tự đầy từ dữ liệu bể cá, admin chỉ can thiệp để seed hoặc gỡ dòng rác.
 *
 * Trả về null khi không đủ quyền, nên nơi dùng không phải tự kiểm tra.
 */
export default function AddToCountryButton({ specCode, className, variant = 'icon' }: Props) {
  const { t } = useTranslation();
  const roles = useAuthStore((s) => s.roles);
  const isAdmin = ADMIN_ROLES.some((r) => roles.includes(r));

  const country = getStoredCountry();
  const countryName = t(`countries.${country}`);

  const [inList, setInList] = useState<boolean | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!isAdmin) return;
    let cancelled = false;
    getSellingCountries(specCode)
      .then((list) => { if (!cancelled) setInList(list.includes(country)); })
      .catch(() => { if (!cancelled) setInList(false); });
    return () => { cancelled = true; };
  }, [isAdmin, specCode, country]);

  if (!isAdmin) return null;

  const toggle = async (e: React.MouseEvent) => {
    e.stopPropagation();
    e.preventDefault();
    if (busy || inList === null) return;

    const next = !inList;
    setBusy(true);
    setInList(next); // lạc quan — rollback nếu API lỗi
    try {
      if (next) await addTradedSpecies(country, { specCode });
      else await removeTradedSpecies(country, specCode);
    } catch {
      setInList(!next);
    } finally {
      setBusy(false);
    }
  };

  const label = inList
    ? t('market.removeFromCountry', { country: countryName })
    : t('market.addToCountry', { country: countryName });

  return (
    <button
      onClick={toggle}
      disabled={busy || inList === null}
      title={label}
      aria-label={label}
      className={cn(
        // 44px là mốc tối thiểu của Apple HIG — hàng action trên thẻ đã chật nên phải giữ đúng
        'inline-flex items-center justify-center gap-2 min-h-[44px] rounded-lg border transition-colors disabled:opacity-50',
        variant === 'icon' ? 'min-w-[44px] px-3' : 'px-4',
        inList
          ? 'bg-emerald-500/15 border-emerald-500/40 text-emerald-400'
          : 'bg-[#2a2d32] border-transparent text-slate-300 hover:bg-primary/20 hover:border-primary/40 hover:text-primary',
        className,
      )}
    >
      {busy
        ? <Loader2 className="w-4 h-4 animate-spin" />
        : inList ? <Check className="w-4 h-4" /> : <Store className="w-4 h-4" />}
      {variant === 'full' && (
        <span className="text-sm font-medium">
          {inList ? t('market.addedToCountry', { country: countryName }) : label}
        </span>
      )}
    </button>
  );
}
