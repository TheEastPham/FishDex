import { useEffect, useMemo, useState } from 'react';
import {
  createPrizeTier, updatePrizeTier, deletePrizeTier, requestPrizeTierImageUpload,
  createSponsor, deleteSponsor, requestSponsorLogoUpload,
  uploadToR2, getContestLeaderboard, finalizeContest,
  PrizeTierLevel, SponsorTier, ContestStatus, useTranslation,
} from '@fishlover/shared';
import type {
  ContestDto, ContestPrizeTierDto, ContestSponsorDto, LeaderboardEntryDto,
  CreatePrizeTierRequest, CreateSponsorRequest,
} from '@fishlover/shared';
import { Plus, Trash2, Loader2, Upload, ImageIcon, ExternalLink, Trophy, Building2 } from 'lucide-react';

type T = ReturnType<typeof useTranslation>['t'];

const TIER_LEVEL_KEYS: Record<PrizeTierLevel, string> = {
  [PrizeTierLevel.Gold]: 'adminContests.tierLevelGold',
  [PrizeTierLevel.Silver]: 'adminContests.tierLevelSilver',
  [PrizeTierLevel.Bronze]: 'adminContests.tierLevelBronze',
  [PrizeTierLevel.Encouragement]: 'adminContests.tierLevelEncouragement',
  [PrizeTierLevel.Custom]: 'adminContests.tierLevelCustom',
};

const SPONSOR_TIER_KEYS: Record<SponsorTier, string> = {
  [SponsorTier.Platinum]: 'adminContests.sponsorTierPlatinum',
  [SponsorTier.Gold]: 'adminContests.sponsorTierGold',
  [SponsorTier.Silver]: 'adminContests.sponsorTierSilver',
  [SponsorTier.Bronze]: 'adminContests.sponsorTierBronze',
  [SponsorTier.Partner]: 'adminContests.sponsorTierPartner',
};

const inputCls = 'w-full bg-[#0F172A] border border-slate-700 rounded-lg px-2.5 py-2 text-sm text-white min-h-[40px] placeholder:text-slate-600';

// ── Prize tier row — chỉ gọi API lúc blur/Enter, không phải mỗi lần gõ ────────
function PrizeTierRow({ contest, tier, onRemove, onChanged, t }: {
  contest: ContestDto; tier: ContestPrizeTierDto; onRemove: () => void; onChanged: () => void; t: T;
}) {
  const [value, setValue] = useState(String(tier.slotCount));
  const [saving, setSaving] = useState(false);
  const [uploading, setUploading] = useState(false);

  // Đồng bộ lại khi list reload (vd sau khi tier khác thay đổi) và user chưa đang sửa dở
  useEffect(() => { setValue(String(tier.slotCount)); }, [tier.slotCount]);

  const commit = async () => {
    const num = Math.max(0, Number(value) || 0);
    if (num === tier.slotCount) { setValue(String(tier.slotCount)); return; }
    setSaving(true);
    try {
      await updatePrizeTier(contest.id, tier.id, { slotCount: num });
      onChanged();
    } finally {
      setSaving(false);
    }
  };

  const handleImageChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const { uploadUrl } = await requestPrizeTierImageUpload(contest.id, tier.id, file.name, file.type);
      await uploadToR2(uploadUrl, file, file.type);
      onChanged();
    } catch (err) {
      console.error(err);
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="flex items-center gap-2 rounded-lg bg-[#0F172A] border border-slate-800 px-2.5 py-2">
      <div className="w-9 h-9 rounded bg-[#172033] border border-slate-700 flex items-center justify-center shrink-0 overflow-hidden">
        {tier.imageUrl ? <img src={tier.imageUrl} alt="" className="w-full h-full object-cover" /> : <ImageIcon className="w-3.5 h-3.5 text-slate-600" />}
      </div>
      <span className="text-xs text-slate-400 flex-1 truncate">
        {tier.name} <span className="text-slate-600">· {t(TIER_LEVEL_KEYS[tier.tierLevel])}</span>
      </span>
      <input
        type="number"
        min={0}
        value={value}
        onChange={e => setValue(e.target.value)}
        onBlur={commit}
        onKeyDown={e => { if (e.key === 'Enter') (e.target as HTMLInputElement).blur(); }}
        disabled={saving}
        className="w-14 bg-[#172033] border border-slate-700 rounded px-1.5 py-1 text-xs text-white text-center shrink-0 disabled:opacity-50"
      />
      <label className="p-1.5 rounded hover:bg-white/10 text-slate-500 hover:text-white transition-colors shrink-0 cursor-pointer">
        {uploading ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Upload className="w-3.5 h-3.5" />}
        <input type="file" accept="image/jpeg,image/png,image/webp,image/svg+xml" className="hidden" onChange={handleImageChange} disabled={uploading} />
      </label>
      <button
        onClick={onRemove}
        className="p-1.5 rounded hover:bg-red-500/10 text-slate-600 hover:text-red-400 transition-colors shrink-0"
      >
        <Trash2 className="w-3.5 h-3.5" />
      </button>
    </div>
  );
}

// ── Prize tiers ──────────────────────────────────────────────────────────────
function PrizeTiersSection({ contest, tiers, onChanged, t }: {
  contest: ContestDto; tiers: ContestPrizeTierDto[]; onChanged: () => void; t: T;
}) {
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState('');
  const [level, setLevel] = useState<PrizeTierLevel>(PrizeTierLevel.Custom);
  const [slotCount, setSlotCount] = useState(1);
  const [description, setDescription] = useState('');
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [saving, setSaving] = useState(false);

  const addTier = async () => {
    if (!name.trim()) return;
    setSaving(true);
    try {
      const req: CreatePrizeTierRequest = {
        name: name.trim(), tierLevel: level, slotCount,
        description: description.trim() || null,
      };
      const created = await createPrizeTier(contest.id, req);

      // Ảnh chọn sẵn trong form — upload luôn sau khi tier đã có id, không bắt user thao tác thêm lần 2
      if (imageFile) {
        const { uploadUrl } = await requestPrizeTierImageUpload(contest.id, created.id, imageFile.name, imageFile.type);
        await uploadToR2(uploadUrl, imageFile, imageFile.type);
      }

      setName(''); setSlotCount(1); setLevel(PrizeTierLevel.Custom); setDescription(''); setImageFile(null); setAdding(false);
      onChanged();
    } finally {
      setSaving(false);
    }
  };

  const removeTier = async (tier: ContestPrizeTierDto) => {
    if (!window.confirm(t('adminContests.deleteTierConfirm'))) return;
    await deletePrizeTier(contest.id, tier.id);
    onChanged();
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <p className="flex items-center gap-1.5 text-xs font-bold text-slate-400 uppercase tracking-wider">
          <Trophy className="w-3.5 h-3.5" /> {t('adminContests.prizeTiersTitle')}
        </p>
        <button
          onClick={() => setAdding(v => !v)}
          className="flex items-center gap-1 text-xs font-semibold text-sky-400 hover:text-sky-300 min-h-[32px] px-2"
        >
          <Plus className="w-3.5 h-3.5" /> {t('adminContests.addTier')}
        </button>
      </div>

      <div className="space-y-1.5">
        {tiers.map(tier => (
          <PrizeTierRow key={tier.id} contest={contest} tier={tier} onRemove={() => removeTier(tier)} onChanged={onChanged} t={t} />
        ))}
      </div>

      {adding && (
        <div className="mt-2 space-y-2 rounded-lg border border-slate-800 p-3 bg-[#0F172A]">
          <input value={name} onChange={e => setName(e.target.value)} placeholder={t('adminContests.tierName')} className={inputCls} />
          <div className="grid grid-cols-2 gap-2">
            <select value={level} onChange={e => setLevel(Number(e.target.value) as PrizeTierLevel)} className={inputCls}>
              {Object.entries(TIER_LEVEL_KEYS).map(([value, key]) => (
                <option key={value} value={value}>{t(key)}</option>
              ))}
            </select>
            <input
              type="number" min={0} value={slotCount}
              onChange={e => setSlotCount(Number(e.target.value))}
              placeholder={t('adminContests.tierSlotCount')}
              className={inputCls}
            />
          </div>
          <textarea
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder={t('adminContests.tierDescription')}
            rows={2}
            className={inputCls}
          />

          {/* Ảnh giải thưởng — chọn ngay lúc tạo, upload chạy ngay sau khi tier được tạo xong */}
          <label className="flex items-center gap-2 rounded-lg border border-dashed border-slate-700 px-2.5 py-2 cursor-pointer hover:border-sky-500/40 transition-colors">
            <ImageIcon className="w-4 h-4 text-slate-500 shrink-0" />
            <span className="text-xs text-slate-400 truncate flex-1">
              {imageFile ? imageFile.name : t('adminContests.tierImagePick')}
            </span>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,image/svg+xml"
              className="hidden"
              onChange={e => setImageFile(e.target.files?.[0] ?? null)}
            />
          </label>

          <button
            onClick={addTier}
            disabled={saving || !name.trim()}
            className="w-full py-2 rounded-lg bg-sky-500 text-white text-xs font-bold hover:bg-sky-400 disabled:opacity-40 flex items-center justify-center gap-2 min-h-[36px]"
          >
            {saving && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
            {t('adminContests.save')}
          </button>
        </div>
      )}
    </div>
  );
}

// ── Sponsors ─────────────────────────────────────────────────────────────────
function SponsorRow({ contest, sponsor, onChanged, t }: {
  contest: ContestDto; sponsor: ContestSponsorDto; onChanged: () => void; t: T;
}) {
  const [uploading, setUploading] = useState(false);

  const handleLogoChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      const { uploadUrl } = await requestSponsorLogoUpload(contest.id, sponsor.id, file.name, file.type);
      await uploadToR2(uploadUrl, file, file.type);
      onChanged();
    } catch (err) {
      console.error(err);
    } finally {
      setUploading(false);
    }
  };

  const remove = async () => {
    if (!window.confirm(t('adminContests.deleteSponsorConfirm'))) return;
    await deleteSponsor(contest.id, sponsor.id);
    onChanged();
  };

  return (
    <div className="flex items-center gap-2 rounded-lg bg-[#0F172A] border border-slate-800 px-2.5 py-2">
      <div className="w-10 h-10 rounded bg-[#172033] border border-slate-700 flex items-center justify-center shrink-0 overflow-hidden">
        {sponsor.logoUrl ? <img src={sponsor.logoUrl} alt="" className="w-full h-full object-contain" /> : <ImageIcon className="w-4 h-4 text-slate-600" />}
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-xs font-semibold text-white truncate">{sponsor.name}</p>
        <p className="text-[10px] text-slate-500">
          {t(SPONSOR_TIER_KEYS[sponsor.sponsorTier])}
          {sponsor.address && <span className="truncate"> · {sponsor.address}</span>}
        </p>
      </div>
      {sponsor.websiteUrl && (
        <a href={sponsor.websiteUrl} target="_blank" rel="noopener noreferrer" className="p-1.5 text-slate-500 hover:text-sky-400 shrink-0">
          <ExternalLink className="w-3.5 h-3.5" />
        </a>
      )}
      <label className="p-1.5 rounded hover:bg-white/10 text-slate-500 hover:text-white transition-colors shrink-0 cursor-pointer">
        {uploading ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Upload className="w-3.5 h-3.5" />}
        <input type="file" accept="image/jpeg,image/png,image/webp,image/svg+xml" className="hidden" onChange={handleLogoChange} disabled={uploading} />
      </label>
      <button onClick={remove} className="p-1.5 rounded hover:bg-red-500/10 text-slate-600 hover:text-red-400 transition-colors shrink-0">
        <Trash2 className="w-3.5 h-3.5" />
      </button>
    </div>
  );
}

function SponsorsSection({ contest, sponsors, onChanged, t }: {
  contest: ContestDto; sponsors: ContestSponsorDto[]; onChanged: () => void; t: T;
}) {
  const [adding, setAdding] = useState(false);
  const [name, setName] = useState('');
  const [website, setWebsite] = useState('');
  const [address, setAddress] = useState('');
  const [tier, setTier] = useState<SponsorTier>(SponsorTier.Partner);
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [saving, setSaving] = useState(false);

  const addSponsor = async () => {
    if (!name.trim()) return;
    setSaving(true);
    try {
      const req: CreateSponsorRequest = {
        name: name.trim(),
        websiteUrl: website.trim() || null,
        address: address.trim() || null,
        sponsorTier: tier,
      };
      const sponsor = await createSponsor(contest.id, req);

      // Logo chọn sẵn trong form tạo — upload luôn sau khi sponsor đã có id, không bắt user thao tác thêm lần 2
      if (logoFile) {
        const { uploadUrl } = await requestSponsorLogoUpload(contest.id, sponsor.id, logoFile.name, logoFile.type);
        await uploadToR2(uploadUrl, logoFile, logoFile.type);
      }

      setName(''); setWebsite(''); setAddress(''); setTier(SponsorTier.Partner); setLogoFile(null); setAdding(false);
      onChanged();
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-2">
        <p className="flex items-center gap-1.5 text-xs font-bold text-slate-400 uppercase tracking-wider">
          <Building2 className="w-3.5 h-3.5" /> {t('adminContests.sponsorsTitle')}
        </p>
        <button
          onClick={() => setAdding(v => !v)}
          className="flex items-center gap-1 text-xs font-semibold text-sky-400 hover:text-sky-300 min-h-[32px] px-2"
        >
          <Plus className="w-3.5 h-3.5" /> {t('adminContests.addSponsor')}
        </button>
      </div>

      <div className="space-y-1.5">
        {sponsors.map(s => (
          <SponsorRow key={s.id} contest={contest} sponsor={s} onChanged={onChanged} t={t} />
        ))}
      </div>

      {adding && (
        <div className="mt-2 space-y-2 rounded-lg border border-slate-800 p-3 bg-[#0F172A]">
          <input value={name} onChange={e => setName(e.target.value)} placeholder={t('adminContests.sponsorName')} className={inputCls} />
          <input value={website} onChange={e => setWebsite(e.target.value)} placeholder={t('adminContests.sponsorWebsite')} className={inputCls} />
          <input value={address} onChange={e => setAddress(e.target.value)} placeholder={t('adminContests.sponsorAddress')} className={inputCls} />
          <select value={tier} onChange={e => setTier(Number(e.target.value) as SponsorTier)} className={inputCls}>
            {Object.entries(SPONSOR_TIER_KEYS).map(([value, key]) => (
              <option key={value} value={value}>{t(key)}</option>
            ))}
          </select>

          {/* Logo — chọn ngay lúc tạo, upload chạy ngay sau khi sponsor được tạo xong */}
          <label className="flex items-center gap-2 rounded-lg border border-dashed border-slate-700 px-2.5 py-2 cursor-pointer hover:border-sky-500/40 transition-colors">
            <ImageIcon className="w-4 h-4 text-slate-500 shrink-0" />
            <span className="text-xs text-slate-400 truncate flex-1">
              {logoFile ? logoFile.name : t('adminContests.sponsorLogoPick')}
            </span>
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,image/svg+xml"
              className="hidden"
              onChange={e => setLogoFile(e.target.files?.[0] ?? null)}
            />
          </label>

          <button
            onClick={addSponsor}
            disabled={saving || !name.trim()}
            className="w-full py-2 rounded-lg bg-sky-500 text-white text-xs font-bold hover:bg-sky-400 disabled:opacity-40 flex items-center justify-center gap-2 min-h-[36px]"
          >
            {saving && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
            {t('adminContests.save')}
          </button>
        </div>
      )}
    </div>
  );
}

// ── Finalize ─────────────────────────────────────────────────────────────────
function FinalizeSection({ contest, tiers, onChanged, t }: {
  contest: ContestDto; tiers: ContestPrizeTierDto[]; onChanged: () => void; t: T;
}) {
  const [entries, setEntries] = useState<LeaderboardEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [assignments, setAssignments] = useState<Record<string, string | null>>({});
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(false);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    getContestLeaderboard(contest.id)
      .then(list => {
        setEntries(list);
        setAssignments(Object.fromEntries(list.map(e => [e.entryId, e.prizeTierId])));
      })
      .catch(() => setEntries([]))
      .finally(() => setLoading(false));
  }, [contest.id]);

  const usedPerTier = useMemo(() => {
    const map: Record<string, number> = {};
    Object.values(assignments).forEach(tierId => {
      if (tierId) map[tierId] = (map[tierId] ?? 0) + 1;
    });
    return map;
  }, [assignments]);

  const isEnded = contest.status === ContestStatus.Ended;

  const submit = async () => {
    if (!window.confirm(t('adminContests.finalizeConfirm'))) return;
    setSubmitting(true);
    setError(false);
    try {
      await finalizeContest(contest.id, {
        assignments: entries.map(e => ({ entryId: e.entryId, prizeTierId: assignments[e.entryId] ?? null })),
      });
      setSuccess(true);
      onChanged();
    } catch {
      setError(true);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <div className="flex items-center justify-center py-6 text-slate-600"><Loader2 className="w-5 h-5 animate-spin" /></div>;
  }

  return (
    <div>
      <p className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-1">{t('adminContests.finalizeTitle')}</p>
      <p className="text-[11px] text-slate-500 mb-3">{t('adminContests.finalizeHint')}</p>

      {isEnded && (
        <div className="mb-3 rounded-lg bg-slate-500/10 border border-slate-500/30 px-3 py-2 text-xs text-slate-400">
          {t('adminContests.alreadyFinalized')}
        </div>
      )}
      {success && (
        <div className="mb-3 rounded-lg bg-emerald-500/10 border border-emerald-500/30 px-3 py-2 text-xs text-emerald-400">
          {t('adminContests.finalizeSuccess')}
        </div>
      )}
      {error && (
        <div className="mb-3 rounded-lg bg-red-500/10 border border-red-500/30 px-3 py-2 text-xs text-red-400">
          {t('adminContests.finalizeError')}
        </div>
      )}

      {entries.length === 0 && (
        <p className="text-xs text-slate-600 py-4 text-center">{t('adminContests.finalizeNoPublished')}</p>
      )}

      <div className="space-y-1.5">
        {entries.map((entry, i) => (
          <div key={entry.entryId} className="flex items-center gap-2 rounded-lg bg-[#0F172A] border border-slate-800 px-2.5 py-2">
            <span className="text-xs text-slate-500 w-16 shrink-0">#{i + 1} · {entry.youTubeViewCount.toLocaleString()}v</span>
            <select
              value={assignments[entry.entryId] ?? ''}
              onChange={e => setAssignments(prev => ({ ...prev, [entry.entryId]: e.target.value || null }))}
              disabled={isEnded}
              className="flex-1 bg-[#172033] border border-slate-700 rounded px-2 py-1.5 text-xs text-white disabled:opacity-50"
            >
              <option value="">{t('adminContests.finalizeNoAward')}</option>
              {tiers.map(tier => {
                const used = usedPerTier[tier.id] ?? 0;
                const isCurrent = assignments[entry.entryId] === tier.id;
                const full = used >= tier.slotCount && !isCurrent;
                return (
                  <option key={tier.id} value={tier.id} disabled={full}>
                    {tier.name} ({t('adminContests.finalizeSlotLabel', { used, total: tier.slotCount })}){full ? ' ✕' : ''}
                  </option>
                );
              })}
            </select>
          </div>
        ))}
      </div>

      {!isEnded && entries.length > 0 && (
        <button
          onClick={submit}
          disabled={submitting}
          className="w-full mt-3 py-2.5 rounded-lg bg-amber-500 text-amber-950 text-xs font-bold hover:bg-amber-400 disabled:opacity-40 flex items-center justify-center gap-2 min-h-[40px]"
        >
          {submitting && <Loader2 className="w-3.5 h-3.5 animate-spin" />}
          {t('adminContests.finalizeSubmit')}
        </button>
      )}
    </div>
  );
}

// ── Panel ──────────────────────────────────────────────────────────────────
export default function ContestManagePanel({ contest, onChanged }: { contest: ContestDto; onChanged: () => void }) {
  const { t } = useTranslation();

  return (
    <div className="rounded-xl border border-slate-800/60 bg-[#1E293B] p-4 space-y-5">
      <PrizeTiersSection contest={contest} tiers={contest.prizeTiers} onChanged={onChanged} t={t} />
      <SponsorsSection contest={contest} sponsors={contest.sponsors} onChanged={onChanged} t={t} />
      <FinalizeSection contest={contest} tiers={contest.prizeTiers} onChanged={onChanged} t={t} />
    </div>
  );
}
