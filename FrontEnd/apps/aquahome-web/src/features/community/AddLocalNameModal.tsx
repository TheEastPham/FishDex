import { useState } from 'react';
import { submitCommonName, useTranslation } from '@fishlover/shared';
import { Loader2, X, Check, Languages } from 'lucide-react';

interface Props {
  specCode: number;
  onClose: () => void;
}

const inputCls =
  'w-full rounded-xl bg-[#0F172A] border border-slate-700 px-4 py-3 text-base text-slate-200 ' +
  'placeholder:text-slate-500 focus:outline-none focus:border-sky-500 min-h-[44px]';

export default function AddLocalNameModal({ specCode, onClose }: Props) {
  const { t } = useTranslation();
  const [comName, setComName] = useState('');
  const [language, setLanguage] = useState('Vietnamese');
  const [country, setCountry] = useState('');
  const [transliteration, setTransliteration] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const handleSubmit = async () => {
    if (!comName.trim()) return;
    setSaving(true);
    setError(null);
    try {
      await submitCommonName(specCode, {
        comName: comName.trim(),
        language,
        transliteration: transliteration.trim() || null,
        countryCode: country.trim() || null,
      });
      setDone(true);
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
                <p className="text-sm text-slate-400 mb-4">{t('contribute.addLocalNameHint')}</p>
                {error && (
                  <div className="rounded-xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-400 mb-4">
                    {error}
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

                <div className="grid grid-cols-2 gap-3 mt-4">
                  <div>
                    <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5">
                      {t('contribute.languageLabel')}
                    </label>
                    <select className={inputCls} value={language} onChange={(e) => setLanguage(e.target.value)}>
                      <option value="Vietnamese">{t('contribute.langVietnamese')}</option>
                      <option value="English">{t('contribute.langEnglish')}</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5">
                      {t('contribute.countryLabel')}
                    </label>
                    <input
                      className={inputCls}
                      value={country}
                      maxLength={2}
                      onChange={(e) => setCountry(e.target.value.toUpperCase())}
                      placeholder={t('contribute.countryPlaceholder')}
                    />
                  </div>
                </div>

                <label className="block text-xs font-bold text-slate-500 uppercase tracking-wider mb-1.5 mt-4">
                  {t('contribute.transliterationLabel')}
                </label>
                <input
                  className={inputCls}
                  value={transliteration}
                  onChange={(e) => setTransliteration(e.target.value)}
                />

                <div className="flex gap-3 mt-6">
                  <button
                    onClick={onClose}
                    className="flex-1 py-3 rounded-xl border border-slate-700 text-slate-400 text-sm font-bold hover:bg-white/5 min-h-[44px]"
                  >
                    {t('contribute.cancel')}
                  </button>
                  <button
                    onClick={handleSubmit}
                    disabled={saving || !comName.trim()}
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
