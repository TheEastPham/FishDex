import type { MarketStatsDto } from '@fishlover/shared';
import { cn, useTranslation } from '@fishlover/shared';

interface Props {
  stats: MarketStatsDto | null;
  loading?: boolean;
  /** Bấm ô "chờ đặt tên" là lọc luôn sang nhóm chưa có tên — biến con số thành việc làm được. */
  onAwaitingClick?: () => void;
}

/**
 * Ba con số tiến độ.
 *
 * Ô được nhấn là "chờ đặt tên" chứ không phải "đã có tên", vì tiếng Việt trong FishBase gần như
 * trống nên con số đó sẽ lớn. Cùng một sự thật nhưng đọc thành lời mời thay vì lời thú nhận.
 */
export default function MarketStats({ stats, loading, onAwaitingClick }: Props) {
  const { t } = useTranslation();

  if (loading || !stats) {
    return (
      <div className="grid grid-cols-3 gap-2">
        {[0, 1, 2].map((i) => (
          <div key={i} className="h-[68px] rounded-xl bg-[#202226] animate-pulse" />
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-3 gap-2">
      <Stat label={t('market.statTraded')} value={stats.traded} />
      <Stat label={t('market.statNamed')} value={stats.withLocalName} />
      <Stat
        label={t('market.statAwaiting')}
        value={stats.awaitingName}
        highlight
        onClick={onAwaitingClick}
      />
    </div>
  );
}

interface StatProps {
  label: string;
  value: number;
  highlight?: boolean;
  onClick?: () => void;
}

function Stat({ label, value, highlight, onClick }: StatProps) {
  const interactive = Boolean(onClick);

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={!interactive}
      className={cn(
        'rounded-xl px-3 py-2.5 text-left transition-colors',
        highlight
          ? 'bg-amber-500/10 border border-amber-500/30'
          : 'bg-[#202226] border border-transparent',
        interactive && 'hover:bg-amber-500/20 cursor-pointer',
        !interactive && 'cursor-default',
      )}
    >
      <p className={cn('text-[11px] leading-tight', highlight ? 'text-amber-300/80' : 'text-slate-400')}>
        {label}
      </p>
      <p className={cn('text-xl font-semibold tabular-nums mt-0.5', highlight ? 'text-amber-400' : 'text-white')}>
        {value.toLocaleString()}
      </p>
    </button>
  );
}
