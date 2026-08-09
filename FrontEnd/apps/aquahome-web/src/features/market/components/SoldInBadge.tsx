import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation, getSellingCountries } from '@fishlover/shared';
import { Store } from 'lucide-react';

interface Props {
  specCode: number;
}

/**
 * Badge "có bán ở ‹nước›" trên trang chi tiết loài — điểm nối duy nhất từ trang tra cứu khoa học
 * sang lớp market.
 *
 * Cố ý chỉ là một chỉ báo, không đổi hành vi gì khác của trang: `/fish` giữ nguyên vai trò tra cứu.
 * Không có nước nào bán thì không render gì cả, để không thêm nhiễu cho phần lớn loài.
 */
export default function SoldInBadge({ specCode }: Props) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [countries, setCountries] = useState<string[]>([]);

  useEffect(() => {
    let cancelled = false;
    getSellingCountries(specCode)
      .then((list) => { if (!cancelled) setCountries(list); })
      .catch(() => { if (!cancelled) setCountries([]); });
    return () => { cancelled = true; };
  }, [specCode]);

  if (countries.length === 0) return null;

  return (
    <div className="flex flex-wrap justify-center gap-2 mt-4">
      {countries.map((alpha2) => (
        <button
          key={alpha2}
          onClick={() => navigate(`/market/${alpha2.toLowerCase()}`)}
          className="inline-flex items-center gap-1.5 rounded-full border border-emerald-500/35 bg-emerald-500/10 px-3 py-1.5 text-xs font-medium text-emerald-300 hover:bg-emerald-500/20 transition-colors min-h-[36px]"
        >
          <Store className="w-3.5 h-3.5" />
          {t('market.soldIn', { country: t(`countries.${alpha2}`) })}
        </button>
      ))}
    </div>
  );
}
