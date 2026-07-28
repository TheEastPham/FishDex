import { useEffect, useState } from 'react';
import { submitCommonName, updateCommonName, getMyCommonNames, useTranslation } from '@fishlover/shared';
import type { CommunityCommonNameDto } from '@fishlover/shared';
import { Loader2, X, Languages, Pencil } from 'lucide-react';
import ResultModal from '@/components/common/ResultModal';

interface Props {
  specCode: number;
  onClose: () => void;
  /** Ngôn ngữ mở sẵn (vd khi bấm Sửa từ 1 dòng cụ thể trong "Đóng góp của tôi"). */
  initialLanguage?: string;
  /** Gọi lại sau khi submit/update thành công — dùng để refresh list ở trang cha. */
  onSaved?: () => void;
}

const inputCls =
  'w-full rounded-xl bg-[#0F172A] border border-slate-700 px-4 py-3 text-base text-slate-200 ' +
  'placeholder:text-slate-500 focus:outline-none focus:border-sky-500 min-h-[44px]';

const isPending = (n: CommunityCommonNameDto) => !n.isVerified && !n.rejectionReason;

export default function AddLocalNameModal({ specCode, onClose, initialLanguage = 'Vietnamese', onSaved }: Props) {
  const { t } = useTranslation();
  const [comName, setComName] = useState('');
  const [language, setLanguage] = useState(initialLanguage);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [loadingMine, setLoadingMine] = useState(true);
  const [pendingForLang, setPendingForLang] = useState<CommunityCommonNameDto | null>(null);
  const isEditing = !!pendingForLang;

  useEffect(() => {
    setLoadingMine(true);
    getMyCommonNames()
      .then((mine) => {
        const p = mine.find((n) => n.specCode === specCode && n.language === language && isPending(n)) ?? null;
        setPendingForLang(p);
        if (p) setComName(p.comName);
      })
      .finally(() => setLoadingMine(false));
  }, [specCode, language]);

  if (done) {
    return <ResultModal title={t(isEditing ? 'contribute.updated' : 'contribute.submitted')} onClose={onClose} />;
  }

  const handleSubmit = async () => {
    if (!comName.trim()) return;
    setSaving(true);
    setError(null);
    try {
      if (pendingForLang) await updateCommonName(pendingForLang.autoCtr, comName.trim());
      else await submitCommonName(specCode, { comName: comName.trim(), language });
      setDone(true);
      onSaved?.();
    } catch (e) {
      const status = (e as { response?: { status?: number } })?.response?.status;
      setError(status === 409 ? t('contribute.errorDuplicate') : t('contribute.error'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <div className="fixed inset-0 bg-black/60 z-50" onClick={onClose} />
      <div className="fixed inset-0 z-50 flex items-center justify-center p-4 pointer-events-none">
        <div className="w-full max-w-md max-h-[90vh] overflow-y-auto bg-[#172033] border border-slate-700 rounded-2xl pointer-events-auto">
          <div className="sticky top-0 bg-[#172033] flex items-center justify-between px-5 py-4 border-b border-slate-800 z-10">
            <h2 className="text-base font-bold text-white flex items-center gap-2">
              <Languages className="w-4 h-4 text-sky-400" />
              {t('contribute.addLocalNameTitle')}
            </h2>
            <button
              onClick={onClose}
              className="p-2 rounded-lg hover:bg-white/10 text-slate-500 min-w-[44px] min-h-[44px] flex items-center justify-center"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="p-5">
            <p className="text-sm text-slate-400 mb-4">{t('contribute.addLocalNameHint')}</p>
            {error && (
              <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400 mb-4">
                {error}
              </div>
            )}

                {isEditing && (
                  <div className="flex items-start gap-2 rounded-xl border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-300 mb-4">
                    <Pencil className="w-4 h-4 shrink-0 mt-0.5" />
                    <span>{t('contribute.editingPending')}</span>
                  </div>
                )}

                <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5">
                  {t('contribute.nameLabel')}
                </label>
                <input
                  className={inputCls}
                  value={comName}
                  onChange={(e) => setComName(e.target.value)}
                  placeholder={t('contribute.namePlaceholder')}
                  autoFocus
                />

                <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5 mt-4">
                  {t('contribute.languageLabel')}
                </label>
                <select className={inputCls} value={language} onChange={(e) => setLanguage(e.target.value)}>
                  <option value="Vietnamese">{t('contribute.langVietnamese')}</option>
                  <option value="English">{t('contribute.langEnglish')}</option>
                </select>

                <div className="flex gap-3 mt-6">
                  <button
                    onClick={onClose}
                    className="flex-1 py-3 rounded-xl border border-slate-700 text-slate-400 text-sm font-bold hover:bg-white/5 min-h-[44px]"
                  >
                    {t('contribute.cancel')}
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={saving || loadingMine || !comName.trim()}
                    className="flex-1 py-3 rounded-xl bg-sky-500 text-white text-sm font-bold hover:bg-sky-400 disabled:opacity-50 flex items-center justify-center gap-2 min-h-[44px]"
                  >
                    {saving && <Loader2 className="w-4 h-4 animate-spin" />}
                    {saving ? t('contribute.submitting') : t(isEditing ? 'contribute.update' : 'contribute.submit')}
                  </button>
                </div>
          </div>
        </div>
      </div>
    </>
  );
}
