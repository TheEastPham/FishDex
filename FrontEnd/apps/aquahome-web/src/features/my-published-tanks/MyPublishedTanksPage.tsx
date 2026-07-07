import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getMySnapshots, unpublishSnapshot, useTranslation, cn } from '@fishlover/shared';
import type { MySnapshotDto } from '@fishlover/shared';
import { Heart, Fish, Loader2, Waves, ExternalLink, Copy, Check, Trash2 } from 'lucide-react';

function formatDate(iso: string, locale: string) {
  return new Date(iso).toLocaleDateString(locale, { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function SnapshotRow({ snapshot, onUnpublished, t, dateLocale }: {
  snapshot: MySnapshotDto;
  onUnpublished: (id: string) => void;
  t: ReturnType<typeof useTranslation>['t'];
  dateLocale: string;
}) {
  const navigate = useNavigate();
  const [copied, setCopied] = useState(false);
  const [busy, setBusy] = useState(false);

  const publicUrl = `${window.location.origin}/public/tanks/${snapshot.slug}`;

  const copyUrl = async () => {
    await navigator.clipboard.writeText(publicUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleUnpublish = async () => {
    if (!window.confirm(t('myPublished.unpublishConfirm'))) return;
    setBusy(true);
    try {
      await unpublishSnapshot(snapshot.id);
      onUnpublished(snapshot.id);
    } catch (err) {
      console.error(err);
      setBusy(false);
    }
  };

  return (
    <div className="flex flex-col sm:flex-row gap-3 sm:items-center rounded-xl border border-slate-800/60 bg-[#1E293B] p-3">
      {/* Cover */}
      <button
        onClick={() => navigate(`/public/tanks/${snapshot.slug}`)}
        className="w-full sm:w-20 h-32 sm:h-20 rounded-lg overflow-hidden bg-slate-800 shrink-0"
      >
        {snapshot.coverImageUrl
          ? <img src={snapshot.coverImageUrl} alt="" className="w-full h-full object-cover" />
          : <div className="w-full h-full flex items-center justify-center"><Waves className="w-6 h-6 text-slate-600" /></div>
        }
      </button>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-bold text-white truncate">{snapshot.aquariumName}</p>
        <p className="text-xs text-sky-400 truncate">/{snapshot.slug}</p>
        <div className="flex items-center gap-3 mt-1.5 text-xs text-slate-500">
          <span className="flex items-center gap-1">
            <Heart className="w-3.5 h-3.5" /> {snapshot.likeCount}
          </span>
          <span className="flex items-center gap-1">
            <Fish className="w-3.5 h-3.5" /> {snapshot.fishSpeciesCount} {t('aquarium.speciesUnit')}
          </span>
          <span>{t('myPublished.updatedAt')}: {formatDate(snapshot.updatedAt, dateLocale)}</span>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-2 shrink-0">
        <button
          onClick={() => navigate(`/public/tanks/${snapshot.slug}`)}
          className="p-2.5 rounded-lg bg-white/5 hover:bg-white/10 text-slate-400 transition-colors min-w-[44px] min-h-[44px] flex items-center justify-center"
          title={t('myPublished.viewPublic')}
        >
          <ExternalLink className="w-4 h-4" />
        </button>
        <button
          onClick={copyUrl}
          className={cn(
            'p-2.5 rounded-lg transition-colors min-w-[44px] min-h-[44px] flex items-center justify-center',
            copied ? 'bg-emerald-500/20 text-emerald-400' : 'bg-white/5 hover:bg-white/10 text-slate-400',
          )}
          title={t('myPublished.copyLink')}
        >
          {copied ? <Check className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
        </button>
        <button
          onClick={handleUnpublish}
          disabled={busy}
          className="p-2.5 rounded-lg bg-red-500/10 hover:bg-red-500/20 text-red-400 transition-colors disabled:opacity-40 min-w-[44px] min-h-[44px] flex items-center justify-center"
          title={t('myPublished.unpublish')}
        >
          {busy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
        </button>
      </div>
    </div>
  );
}

export default function MyPublishedTanksPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();

  const [snapshots, setSnapshots] = useState<MySnapshotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    getMySnapshots()
      .then(setSnapshots)
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="px-4 sm:px-6 pt-5 pb-10">
      <div className="mb-6">
        <h1 className="text-xl sm:text-2xl font-black text-white">{t('myPublished.title')}</h1>
        <p className="text-sm text-slate-500 mt-1">{t('myPublished.subtitle')}</p>
      </div>

      {loading && (
        <div className="flex items-center justify-center py-20 text-slate-600">
          <Loader2 className="w-6 h-6 animate-spin" />
        </div>
      )}

      {!loading && error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-400">
          {t('myPublished.loadError')}
        </div>
      )}

      {!loading && !error && snapshots.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <Waves className="w-12 h-12 text-slate-700 mb-3" />
          <p className="text-slate-500 text-sm">{t('myPublished.empty')}</p>
          <button
            onClick={() => navigate('/tanks')}
            className="mt-4 px-4 py-2.5 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 transition-colors min-h-[44px]"
          >
            {t('myPublished.goToTanks')}
          </button>
        </div>
      )}

      {!loading && !error && snapshots.length > 0 && (
        <div className="space-y-2">
          {snapshots.map(s => (
            <SnapshotRow
              key={s.id}
              snapshot={s}
              dateLocale={t('aquarium.dateLocale')}
              onUnpublished={id => setSnapshots(prev => prev.filter(x => x.id !== id))}
              t={t}
            />
          ))}
        </div>
      )}
    </div>
  );
}
