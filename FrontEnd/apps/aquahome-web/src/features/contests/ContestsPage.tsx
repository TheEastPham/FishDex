import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getActiveContests, getContestLeaderboard, useAuthStore, useTranslation, cn } from '@fishlover/shared';
import type { ContestDto, LeaderboardEntryDto } from '@fishlover/shared';
import { Trophy, Youtube, Eye, Loader2, Upload, Medal, CalendarRange } from 'lucide-react';
import ContestEntryFormModal from './components/ContestEntryFormModal';

function formatDate(iso: string, locale: string) {
  return new Date(iso).toLocaleDateString(locale, { day: '2-digit', month: '2-digit', year: 'numeric' });
}

const RANK_STYLES: Record<number, string> = {
  1: 'bg-amber-500/20 text-amber-400 border-amber-500/40',
  2: 'bg-slate-400/20 text-slate-300 border-slate-400/40',
  3: 'bg-orange-700/20 text-orange-400 border-orange-700/40',
};

function LeaderboardRow({ entry, index, t }: {
  entry: LeaderboardEntryDto;
  index: number;
  t: ReturnType<typeof useTranslation>['t'];
}) {
  const position = entry.rank ?? index + 1;
  const rankStyle = RANK_STYLES[position] ?? 'bg-white/5 text-slate-500 border-white/10';

  return (
    <div className="flex items-center gap-3 px-3 py-2.5 rounded-xl hover:bg-white/5 transition-colors">
      <span className={cn('w-8 h-8 rounded-lg border flex items-center justify-center text-xs font-black shrink-0', rankStyle)}>
        {position <= 3 ? <Medal className="w-4 h-4" /> : position}
      </span>
      <div className="flex-1 min-w-0">
        <p className="text-sm text-white font-semibold truncate">
          {t('contests.entryN', { n: index + 1 })}
        </p>
        <p className="text-xs text-slate-500 flex items-center gap-1">
          <Eye className="w-3 h-3" /> {entry.youTubeViewCount.toLocaleString()} {t('contests.views')}
        </p>
      </div>
      {entry.youTubeVideoId && (
        <a
          href={`https://www.youtube.com/watch?v=${entry.youTubeVideoId}`}
          target="_blank"
          rel="noopener noreferrer"
          className="p-2.5 rounded-lg bg-red-500/10 border border-red-500/30 text-red-400 hover:bg-red-500/20 transition-colors shrink-0 min-w-[44px] min-h-[44px] flex items-center justify-center"
          title={t('contests.watchVideo')}
        >
          <Youtube className="w-4 h-4" />
        </a>
      )}
    </div>
  );
}

function ContestCard({ contest, t, isAuthenticated, onEnter, onLogin }: {
  contest: ContestDto;
  t: ReturnType<typeof useTranslation>['t'];
  isAuthenticated: boolean;
  onEnter: () => void;
  onLogin: () => void;
}) {
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntryDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getContestLeaderboard(contest.id)
      .then(setLeaderboard)
      .catch(() => setLeaderboard([]))
      .finally(() => setLoading(false));
  }, [contest.id]);

  return (
    <div className="rounded-2xl border border-slate-800/60 bg-[#1E293B] overflow-hidden">
      {/* Header */}
      <div className="relative bg-gradient-to-br from-amber-950 via-orange-900/60 to-slate-950 p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2 mb-1.5">
              <Trophy className="w-4 h-4 text-amber-400 shrink-0" />
              <span className="text-[10px] font-bold uppercase tracking-wider text-amber-400">{t('contests.activeBadge')}</span>
            </div>
            <h2 className="text-lg font-black text-white leading-tight">{contest.title}</h2>
            {contest.description && (
              <p className="text-white/50 text-sm mt-1 line-clamp-2">{contest.description}</p>
            )}
            <p className="flex items-center gap-1.5 text-xs text-white/40 mt-2">
              <CalendarRange className="w-3.5 h-3.5" />
              {formatDate(contest.startAt, t('aquarium.dateLocale'))} → {formatDate(contest.endAt, t('aquarium.dateLocale'))}
            </p>
          </div>
          <button
            onClick={isAuthenticated ? onEnter : onLogin}
            className="flex items-center gap-1.5 px-3.5 py-2.5 rounded-xl bg-amber-500 text-amber-950 text-xs font-bold hover:bg-amber-400 transition-colors shrink-0 min-h-[44px]"
          >
            <Upload className="w-3.5 h-3.5" />
            {isAuthenticated ? t('contests.enterBtn') : t('contests.loginToEnter')}
          </button>
        </div>
      </div>

      {/* Leaderboard */}
      <div className="p-3">
        <p className="text-[10px] font-bold uppercase tracking-wider text-slate-600 px-3 pb-2">
          {t('contests.leaderboard')}
        </p>
        {loading && (
          <div className="flex items-center justify-center py-6 text-slate-600">
            <Loader2 className="w-5 h-5 animate-spin" />
          </div>
        )}
        {!loading && leaderboard.length === 0 && (
          <p className="text-sm text-slate-600 px-3 py-4 text-center">{t('contests.noEntries')}</p>
        )}
        {!loading && leaderboard.map((entry, i) => (
          <LeaderboardRow key={entry.entryId} entry={entry} index={i} t={t} />
        ))}
      </div>
    </div>
  );
}

export default function ContestsPage() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore(s => s.isAuthenticated);

  const [contests, setContests] = useState<ContestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [entryContest, setEntryContest] = useState<ContestDto | null>(null);

  useEffect(() => {
    getActiveContests()
      .then(setContests)
      .catch(() => setError(true))
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="px-4 sm:px-6 pt-5 pb-10">
      <div className="mb-6">
        <h1 className="text-xl sm:text-2xl font-black text-white">{t('contests.title')}</h1>
        <p className="text-sm text-slate-500 mt-1">{t('contests.subtitle')}</p>
      </div>

      {loading && (
        <div className="flex items-center justify-center py-20 text-slate-600">
          <Loader2 className="w-6 h-6 animate-spin" />
        </div>
      )}

      {!loading && error && (
        <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-400">
          {t('contests.loadError')}
        </div>
      )}

      {!loading && !error && contests.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <Trophy className="w-12 h-12 text-slate-700 mb-3" />
          <p className="text-slate-500 text-sm">{t('contests.empty')}</p>
          <p className="text-slate-600 text-xs mt-1">{t('contests.emptyHint')}</p>
        </div>
      )}

      {!loading && !error && contests.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
          {contests.map(c => (
            <ContestCard
              key={c.id}
              contest={c}
              t={t}
              isAuthenticated={isAuthenticated}
              onEnter={() => setEntryContest(c)}
              onLogin={() => navigate('/login')}
            />
          ))}
        </div>
      )}

      {entryContest && (
        <ContestEntryFormModal
          contest={entryContest}
          onClose={() => setEntryContest(null)}
          onSubmitted={() => {}}
        />
      )}
    </div>
  );
}
