import { useEffect, useState } from 'react';
import {
  submitCommunitySpecies, requestCommunitySpeciesImageUpload, uploadToR2, getFamilies,
  useTranslation, WaterType, CommunityCareLevel, CommunitySpeciesKind,
} from '@fishlover/shared';
import type { SubmitCommunitySpeciesRequest, Family } from '@fishlover/shared';
import { Loader2, X, Check, Fish, ImagePlus } from 'lucide-react';

interface Props {
  onClose: () => void;
  /** Prefill tên loài từ ô search khi user không tìm thấy. */
  initialName?: string;
}

const inputCls =
  'w-full rounded-xl bg-[#0F172A] border border-slate-700 px-4 py-3 text-base text-slate-200 ' +
  'placeholder:text-slate-500 focus:outline-none focus:border-sky-500 min-h-[44px]';
const labelCls = 'block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5';

/** '' → undefined; số hợp lệ → number. */
function num(v: string): number | undefined {
  const s = v.trim();
  if (!s) return undefined;
  const n = Number(s);
  return Number.isFinite(n) ? n : undefined;
}

export default function SubmitCommunitySpeciesModal({ onClose, initialName = '' }: Props) {
  const { t } = useTranslation();

  const [speciesName, setSpeciesName] = useState(initialName);
  const [commonName, setCommonName] = useState('');
  const [familyName, setFamilyName] = useState('');
  const [suggestedKind, setSuggestedKind] = useState<CommunitySpeciesKind | ''>('');
  const [waterType, setWaterType] = useState<WaterType>(WaterType.Freshwater);
  const [tempMin, setTempMin] = useState('');
  const [tempMax, setTempMax] = useState('');
  const [phMin, setPhMin] = useState('');
  const [phMax, setPhMax] = useState('');
  const [length, setLength] = useState('');
  const [minTankLiters, setMinTankLiters] = useState('');
  const [careLevel, setCareLevel] = useState<CommunityCareLevel | ''>('');
  const [imageFile, setImageFile] = useState<File | null>(null);

  const [families, setFamilies] = useState<Family[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(false);
  const [done, setDone] = useState(false);

  useEffect(() => {
    getFamilies().then(setFamilies).catch(() => {});
  }, []);

  const canSave = speciesName.trim().length > 0 && !saving;

  const handleSubmit = async () => {
    if (!canSave) return;
    setSaving(true);
    setError(false);
    try {
      const req: SubmitCommunitySpeciesRequest = {
        speciesName: speciesName.trim(),
        waterType,
        commonName: commonName.trim() || null,
        familyName: familyName.trim() || null,
        suggestedKind: suggestedKind === '' ? null : suggestedKind,
        tempMin: num(tempMin) ?? null,
        tempMax: num(tempMax) ?? null,
        phMin: num(phMin) ?? null,
        phMax: num(phMax) ?? null,
        length: num(length) ?? null,
        minTankLiters: num(minTankLiters) ?? null,
        careLevel: careLevel === '' ? null : careLevel,
      };
      const created = await submitCommunitySpecies(req);
      if (imageFile) {
        const { uploadUrl } = await requestCommunitySpeciesImageUpload(created.specCode, imageFile.name, imageFile.type);
        await uploadToR2(uploadUrl, imageFile, imageFile.type);
      }
      setDone(true);
    } catch {
      setError(true);
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
        <div className="w-full max-w-lg max-h-[90vh] overflow-y-auto bg-[#172033] border border-slate-700 rounded-2xl pointer-events-auto">
          <div className="sticky top-0 bg-[#172033] flex items-center justify-between px-5 py-4 border-b border-slate-800 z-10">
            <h2 className="text-base font-bold text-white flex items-center gap-2">
              <Fish className="w-4 h-4 text-sky-400" />
              {t('contribute.addSpeciesTitle')}
            </h2>
            <button
              onClick={onClose}
              className="p-2 rounded-lg hover:bg-white/10 text-slate-500 min-w-[44px] min-h-[44px] flex items-center justify-center"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="p-5">
            {done ? (
              <div className="text-center py-6">
                <div className="w-14 h-14 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center mx-auto mb-4">
                  <Check className="w-7 h-7 text-emerald-400" />
                </div>
                <p className="text-white font-bold mb-5">{t('contribute.submitted')}</p>
                <button
                  onClick={onClose}
                  className="px-6 py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 min-h-[44px]"
                >
                  {t('contribute.cancel')}
                </button>
              </div>
            ) : (
              <>
                <p className="text-sm text-slate-400 mb-4">{t('contribute.addSpeciesHint')}</p>
                {error && (
                  <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400 mb-4">
                    {t('contribute.error')}
                  </div>
                )}

                <div className="space-y-4">
                  <div>
                    <label className={labelCls}>{t('contribute.speciesNameLabel')}</label>
                    <input className={inputCls} value={speciesName} onChange={(e) => setSpeciesName(e.target.value)}
                      placeholder={t('contribute.speciesNamePlaceholder')} autoFocus />
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className={labelCls}>{t('contribute.commonNameLabel')}</label>
                      <input className={inputCls} value={commonName} onChange={(e) => setCommonName(e.target.value)} />
                    </div>
                    <div>
                      <label className={labelCls}>{t('contribute.waterTypeLabel')}</label>
                      <select className={inputCls} value={waterType} onChange={(e) => setWaterType(Number(e.target.value) as WaterType)}>
                        <option value={WaterType.Freshwater}>{t('contribute.waterFreshwater')}</option>
                        <option value={WaterType.Saltwater}>{t('contribute.waterSaltwater')}</option>
                        <option value={WaterType.Brackish}>{t('contribute.waterBrackish')}</option>
                      </select>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className={labelCls}>{t('contribute.familyLabel')}</label>
                      <select className={inputCls} value={familyName} onChange={(e) => setFamilyName(e.target.value)}>
                        <option value="">{t('contribute.familyNone')}</option>
                        {families.map((f) => (
                          <option key={f.id} value={f.name}>{f.name}</option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className={labelCls}>{t('contribute.kindLabel')}</label>
                      <select className={inputCls} value={suggestedKind}
                        onChange={(e) => setSuggestedKind(e.target.value === '' ? '' : (Number(e.target.value) as CommunitySpeciesKind))}>
                        <option value="">{t('contribute.kindNone')}</option>
                        <option value={CommunitySpeciesKind.Natural}>{t('contribute.kindNatural')}</option>
                        <option value={CommunitySpeciesKind.Hybrid}>{t('contribute.kindHybrid')}</option>
                      </select>
                    </div>
                  </div>

                  <div>
                    <label className={labelCls}>{t('contribute.imageLabel')}</label>
                    <label className="flex items-center gap-2 rounded-xl bg-[#0F172A] border border-slate-700 px-4 py-3 text-sm text-slate-300 cursor-pointer hover:border-sky-500 min-h-[44px]">
                      <ImagePlus className="w-4 h-4 text-sky-400 shrink-0" />
                      <span className="truncate">{imageFile ? imageFile.name : t('contribute.imagePick')}</span>
                      <input type="file" accept="image/*" className="hidden"
                        onChange={(e) => setImageFile(e.target.files?.[0] ?? null)} />
                    </label>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className={labelCls}>{t('contribute.tempLabel')}</label>
                      <div className="flex gap-2">
                        <input className={inputCls} inputMode="decimal" value={tempMin} onChange={(e) => setTempMin(e.target.value)} placeholder={t('contribute.minPlaceholder')} />
                        <input className={inputCls} inputMode="decimal" value={tempMax} onChange={(e) => setTempMax(e.target.value)} placeholder={t('contribute.maxPlaceholder')} />
                      </div>
                    </div>
                    <div>
                      <label className={labelCls}>{t('contribute.phLabel')}</label>
                      <div className="flex gap-2">
                        <input className={inputCls} inputMode="decimal" value={phMin} onChange={(e) => setPhMin(e.target.value)} placeholder={t('contribute.minPlaceholder')} />
                        <input className={inputCls} inputMode="decimal" value={phMax} onChange={(e) => setPhMax(e.target.value)} placeholder={t('contribute.maxPlaceholder')} />
                      </div>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className={labelCls}>{t('contribute.sizeLabel')}</label>
                      <input className={inputCls} inputMode="decimal" value={length} onChange={(e) => setLength(e.target.value)} />
                    </div>
                    <div>
                      <label className={labelCls}>{t('contribute.minTankLabel')}</label>
                      <input className={inputCls} inputMode="numeric" value={minTankLiters} onChange={(e) => setMinTankLiters(e.target.value)} />
                    </div>
                  </div>

                  <div>
                    <label className={labelCls}>{t('contribute.careLevelLabel')}</label>
                    <select className={inputCls} value={careLevel}
                      onChange={(e) => setCareLevel(e.target.value === '' ? '' : (Number(e.target.value) as CommunityCareLevel))}>
                      <option value="">—</option>
                      <option value={CommunityCareLevel.Beginner}>{t('contribute.careBeginner')}</option>
                      <option value={CommunityCareLevel.Intermediate}>{t('contribute.careIntermediate')}</option>
                      <option value={CommunityCareLevel.Expert}>{t('contribute.careExpert')}</option>
                    </select>
                  </div>
                </div>

                <div className="flex gap-3 mt-6">
                  <button
                    onClick={onClose}
                    className="flex-1 py-3 rounded-xl border border-slate-700 text-slate-400 text-sm font-bold hover:bg-white/5 min-h-[44px]"
                  >
                    {t('contribute.cancel')}
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={!canSave}
                    className="flex-1 py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 disabled:opacity-50 flex items-center justify-center gap-2 min-h-[44px]"
                  >
                    {saving && <Loader2 className="w-4 h-4 animate-spin" />}
                    {saving ? t('contribute.submitting') : t('contribute.submit')}
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
}
