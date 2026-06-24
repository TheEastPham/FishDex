import { useEffect, useState } from 'react';
import { getMyProfile, updateMyProfile, changePassword, useTranslation, usePushNotification } from '@fishlover/shared';
import type { UserProfileDto } from '@fishlover/shared';
import { Bell, BellOff } from 'lucide-react';

const LANGUAGES = [
  { value: 'vi-VN', label: 'Tiếng Việt' },
  { value: 'en-US', label: 'English' },
  { value: 'es-ES', label: 'Español' },
  { value: 'ja-JP', label: '日本語' },
  { value: 'ko-KR', label: '한국어' },
  { value: 'zh-CN', label: '中文简体' },
  { value: 'zh-TW', label: '中文繁體' },
  { value: 'fr-FR', label: 'Français' },
  { value: 'de-DE', label: 'Deutsch' },
  { value: 'pt-BR', label: 'Português' },
];

export default function ProfilePage() {
  const { t } = useTranslation();
  const [profile, setProfile] = useState<UserProfileDto | null>(null);
  const { permission, loading: pushLoading, isSupported, subscribe, unsubscribe } = usePushNotification();

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [phone, setPhone] = useState('');
  const [language, setLanguage] = useState('vi-VN');
  const [profileMsg, setProfileMsg] = useState('');
  const [savingProfile, setSavingProfile] = useState(false);

  const [currentPwd, setCurrentPwd] = useState('');
  const [newPwd, setNewPwd] = useState('');
  const [confirmPwd, setConfirmPwd] = useState('');
  const [pwdMsg, setPwdMsg] = useState('');
  const [pwdError, setPwdError] = useState('');
  const [savingPwd, setSavingPwd] = useState(false);

  useEffect(() => {
    getMyProfile().then((p) => {
      setProfile(p);
      setFirstName(p.firstName ?? '');
      setLastName(p.lastName ?? '');
      setPhone(p.phoneNumber ?? '');
      setLanguage(p.language ?? 'vi-VN');
    });
  }, []);

  async function handleSaveProfile(e: React.FormEvent) {
    e.preventDefault();
    setSavingProfile(true);
    setProfileMsg('');
    try {
      const updated = await updateMyProfile({ firstName, lastName, phoneNumber: phone, language });
      setProfile(updated);
      setProfileMsg(t('profile.saveSuccess'));
    } finally {
      setSavingProfile(false);
    }
  }

  async function handleChangePassword(e: React.FormEvent) {
    e.preventDefault();
    setPwdError('');
    setPwdMsg('');
    if (newPwd !== confirmPwd) {
      setPwdError('Passwords do not match');
      return;
    }
    setSavingPwd(true);
    try {
      await changePassword({ currentPassword: currentPwd, newPassword: newPwd, confirmPassword: confirmPwd });
      setPwdMsg(t('profile.changePwdSuccess'));
      setCurrentPwd(''); setNewPwd(''); setConfirmPwd('');
    } catch {
      setPwdError('Current password is incorrect');
    } finally {
      setSavingPwd(false);
    }
  }

  if (!profile) {
    return <div className="p-8 text-slate-500 text-sm">Loading...</div>;
  }

  return (
    <div className="max-w-xl mx-auto py-8 px-4 space-y-8">
      {/* Avatar */}
      <div className="flex items-center gap-4">
        <img
          src={profile.avatar ?? ''}
          alt="avatar"
          className="w-20 h-20 rounded-full border border-slate-200 bg-slate-50"
        />
        <div>
          <p className="font-semibold text-slate-800">{profile.fullName}</p>
          <p className="text-slate-500 text-sm">{profile.email}</p>
          {/* TODO: remove when real avatar upload is implemented */}
          <p className="text-xs text-slate-400 mt-1">{t('profile.avatarNote')}</p>
        </div>
      </div>

      {/* Profile form */}
      <form onSubmit={handleSaveProfile} className="bg-white rounded-2xl border border-slate-200 p-6 space-y-4">
        <h2 className="font-semibold text-slate-800">{t('profile.title')}</h2>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.firstName')}</label>
            <input
              type="text"
              required
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.lastName')}</label>
            <input
              type="text"
              required
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.email')}</label>
          <input
            type="email"
            disabled
            value={profile.email}
            className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm bg-slate-50 text-slate-400"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.phone')}</label>
          <input
            type="tel"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
            placeholder="+84..."
            className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.language')}</label>
          <select
            value={language}
            onChange={(e) => setLanguage(e.target.value)}
            className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            {LANGUAGES.map((l) => (
              <option key={l.value} value={l.value}>{l.label}</option>
            ))}
          </select>
        </div>

        {profileMsg && <p className="text-green-600 text-sm">{profileMsg}</p>}

        <button
          type="submit"
          disabled={savingProfile}
          className="w-full bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-medium py-2.5 rounded-xl transition-colors"
        >
          {savingProfile ? t('profile.saving') : t('profile.saveProfile')}
        </button>
      </form>

      {/* Push Notifications */}
      <div className="bg-white rounded-2xl border border-slate-200 p-6 space-y-3">
        <h2 className="font-semibold text-slate-800">{t('profile.pushNotifications')}</h2>
        <p className="text-sm text-slate-500">{t('profile.pushHint')}</p>

        {!isSupported ? (
          <p className="text-sm text-slate-400">{t('profile.pushUnsupported')}</p>
        ) : permission === 'denied' ? (
          <p className="text-sm text-amber-600">{t('profile.pushDenied')}</p>
        ) : permission === 'granted' ? (
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-2 text-sm text-green-600 font-medium">
              <Bell className="w-4 h-4" /> {t('profile.pushEnabled')}
            </span>
            <button
              onClick={unsubscribe}
              disabled={pushLoading}
              className="flex items-center gap-1.5 px-4 py-2 rounded-xl border border-slate-200 text-sm text-slate-600 hover:bg-slate-50 disabled:opacity-50 transition-colors min-h-[44px]"
            >
              <BellOff className="w-4 h-4" />
              {t('profile.pushDisable')}
            </button>
          </div>
        ) : (
          <button
            onClick={subscribe}
            disabled={pushLoading}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-sky-500 hover:bg-sky-600 disabled:opacity-50 text-white text-sm font-medium transition-colors min-h-[44px]"
          >
            <Bell className="w-4 h-4" />
            {pushLoading ? '...' : t('profile.pushEnable')}
          </button>
        )}
      </div>

      {/* Change password */}
      <form onSubmit={handleChangePassword} className="bg-white rounded-2xl border border-slate-200 p-6 space-y-4">
        <h2 className="font-semibold text-slate-800">{t('profile.changePassword')}</h2>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.currentPassword')}</label>
          <input
            type="password"
            required
            value={currentPwd}
            onChange={(e) => setCurrentPwd(e.target.value)}
            className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.newPassword')}</label>
          <input
            type="password"
            required
            minLength={8}
            value={newPwd}
            onChange={(e) => setNewPwd(e.target.value)}
            className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">{t('profile.confirmPassword')}</label>
          <input
            type="password"
            required
            value={confirmPwd}
            onChange={(e) => setConfirmPwd(e.target.value)}
            className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        {pwdError && <p className="text-red-500 text-sm">{pwdError}</p>}
        {pwdMsg && <p className="text-green-600 text-sm">{pwdMsg}</p>}

        <button
          type="submit"
          disabled={savingPwd}
          className="w-full bg-slate-800 hover:bg-slate-900 disabled:opacity-50 text-white font-medium py-2.5 rounded-xl transition-colors"
        >
          {savingPwd ? t('profile.changingPwd') : t('profile.changePwd')}
        </button>
      </form>
    </div>
  );
}
