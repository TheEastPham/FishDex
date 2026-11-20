import { useState } from 'react';
import { cn, useTranslation, SizeBand, NameStatusFilter } from '@fishlover/shared';
import { Filter, X } from 'lucide-react';

export interface MarketFilterValue {
  sizeBand?: SizeBand;
  nameStatus: NameStatusFilter;
}

interface Props {
  value: MarketFilterValue;
  onChange: (next: MarketFilterValue) => void;
  /** Số loài khớp bộ lọc đang chọn — hiện lên nút để không ai áp dụng xong mới thấy rỗng. */
  resultCount?: number;
}

const SIZE_OPTIONS: { band: SizeBand; key: string }[] = [
  { band: SizeBand.Under5, key: 'market.sizeUnder5' },
  { band: SizeBand.From5To10, key: 'market.size5to10' },
  { band: SizeBand.From10To20, key: 'market.size10to20' },
  { band: SizeBand.Over20, key: 'market.sizeOver20' },
];

const NAME_OPTIONS: { status: NameStatusFilter; key: string }[] = [
  { status: NameStatusFilter.All, key: 'market.nameStatusAll' },
  { status: NameStatusFilter.Has, key: 'market.nameStatusHas' },
  { status: NameStatusFilter.Missing, key: 'market.nameStatusMissing' },
];

/**
 * Hai bộ lọc, và cả hai đều dựa trên dữ liệu có thật.
 *
 * Không có bộ lọc mức phổ biến: nguồn dữ liệu chính là bể cá của người dùng, mà dòng sinh theo
 * đường đó không mang `TradeStatus` — chỉ admin curate mới có, nên ngày đầu bộ lọc đó gần như rỗng.
 *
 * Trên mobile mở dạng bottom sheet vì 390px không đủ chỗ bày hết; từ `sm:` trở lên bày thành hàng.
 */
export default function MarketFilters({ value, onChange, resultCount }: Props) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  const activeCount =
    (value.sizeBand !== undefined ? 1 : 0) +
    (value.nameStatus !== NameStatusFilter.All ? 1 : 0);

  const clear = () => onChange({ sizeBand: undefined, nameStatus: NameStatusFilter.All });

  const groups = (
    <>
      <FilterGroup label={t('market.sizeLabel')}>
        {SIZE_OPTIONS.map((o) => (
          <Chip
            key={o.band}
            active={value.sizeBand === o.band}
            // Bấm lại chính nó là bỏ chọn — không cần thêm nút "tất cả" cho nhóm này
            onClick={() => onChange({ ...value, sizeBand: value.sizeBand === o.band ? undefined : o.band })}
          >
            {t(o.key)}
          </Chip>
        ))}
      </FilterGroup>

      <FilterGroup label={t('market.nameStatusLabel')}>
        {NAME_OPTIONS.map((o) => (
          <Chip
            key={o.status}
            active={value.nameStatus === o.status}
            onClick={() => onChange({ ...value, nameStatus: o.status })}
          >
            {t(o.key)}
          </Chip>
        ))}
      </FilterGroup>
    </>
  );

  return (
    <>
      {/* Mobile: nút mở sheet, kèm số bộ lọc đang bật */}
      <button
        onClick={() => setOpen(true)}
        className="sm:hidden inline-flex items-center gap-2 h-11 px-4 rounded-xl bg-primary/15 border border-primary/35 text-sky-300 text-sm font-medium"
      >
        <Filter className="w-4 h-4" />
        {t('market.filters')}
        {activeCount > 0 && <span className="tabular-nums">· {activeCount}</span>}
      </button>

      {/* Desktop: bày thẳng thành hàng */}
      <div className="hidden sm:flex sm:flex-wrap sm:items-end sm:gap-x-6 sm:gap-y-3">{groups}</div>

      {open && (
        <div className="sm:hidden fixed inset-0 z-50 flex items-end bg-black/60" onClick={() => setOpen(false)}>
          <div
            className="w-full rounded-t-2xl bg-[#1a1c20] border-t border-slate-700 p-4 pb-6"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between mb-4">
              <p className="text-base font-semibold text-white">{t('market.filters')}</p>
              <button onClick={() => setOpen(false)} className="p-1.5 text-slate-400 hover:text-white">
                <X className="w-5 h-5" />
              </button>
            </div>

            {groups}

            <div className="flex gap-2 mt-5">
              <button
                onClick={clear}
                className="flex-1 h-11 rounded-xl border border-slate-700 text-sm text-slate-300 hover:bg-slate-800 transition-colors"
              >
                {t('market.filtersClear')}
              </button>
              <button
                onClick={() => setOpen(false)}
                className="flex-1 h-11 rounded-xl bg-primary text-[#04283C] text-sm font-semibold hover:bg-primary/90 transition-colors"
              >
                {t('market.filtersApply', { count: resultCount ?? 0 })}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function FilterGroup({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="mb-4 sm:mb-0">
      <p className="text-xs text-slate-400 mb-2">{label}</p>
      <div className="flex flex-wrap gap-1.5">{children}</div>
    </div>
  );
}

function Chip({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'rounded-lg px-3 py-2 text-[13px] transition-colors min-h-[38px]',
        active
          ? 'bg-primary/15 border border-primary/40 text-sky-300 font-medium'
          : 'bg-[#202226] border border-transparent text-slate-400 hover:text-slate-200',
      )}
    >
      {children}
    </button>
  );
}
