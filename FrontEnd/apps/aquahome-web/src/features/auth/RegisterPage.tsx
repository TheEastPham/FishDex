import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { requestOtp, registerUser, useTranslation } from '@fishlover/shared';

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

export default function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [step, setStep] = useState<1 | 2>(1);
  const [email, setEmail] = useState('');
  const [invitationCode, setInvitationCode] = useState('');
  const [otp, setOtp] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [language, setLanguage] = useState('vi-VN');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSendOtp(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    if (!email.toLowerCase().endsWith('@gmail.com')) {
      setError(t('register.gmailOnly'));
      return;
    }
    setLoading(true);
    try {
      const res = await requestOtp(email, invitationCode);
      if (!res.success) {
        setError(res.message);
      } else {
        setStep(2);
      }
    } catch {
      setError('An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  function validatePassword(pwd: string): string | null {
    if (pwd.length < 8)          return t('register.pwdMinLength');
    if (!/[a-z]/.test(pwd))      return t('register.pwdLowercase');
    if (!/[A-Z]/.test(pwd))      return t('register.pwdUppercase');
    if (!/[0-9]/.test(pwd))      return t('register.pwdDigit');
    return null;
  }

  async function handleRegister(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    const pwdError = validatePassword(password);
    if (pwdError) { setError(pwdError); return; }
    if (password !== confirmPassword) {
      setError(t('register.pwdMismatch'));
      return;
    }
    setLoading(true);
    try {
      const res = await registerUser({
        email,
        password,
        confirmPassword,
        firstName,
        lastName,
        verificationCode: otp,
        language,
      });
      if (!res.success) {
        setError(res.message);
      } else {
        setSuccess(t('register.success'));
        setTimeout(() => navigate('/login'), 2000);
      }
    } catch {
      setError('An error occurred. Please try again.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-8 w-full max-w-sm">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-800">AquaHome 🐠</h1>
          <p className="text-slate-500 text-sm mt-1">
            {step === 1 ? t('register.step1Title') : t('register.step2Title')}
          </p>
        </div>

        {success ? (
          <p className="text-green-600 text-sm text-center">{success}</p>
        ) : step === 1 ? (
          <form onSubmit={handleSendOtp} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('register.email')}
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder={t('register.emailPlaceholder')}
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('register.invitationCode')}
              </label>
              <input
                type="text"
                required
                maxLength={8}
                value={invitationCode}
                onChange={(e) => setInvitationCode(e.target.value.toUpperCase())}
                placeholder={t('register.invitationCodePlaceholder')}
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm font-mono tracking-widest focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            {error && <p className="text-red-500 text-xs">{error}</p>}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 text-white font-medium py-2.5 rounded-xl transition-colors"
            >
              {loading ? t('register.sending') : t('register.sendOtp')}
            </button>

            <p className="text-center text-sm text-slate-500">
              <Link to="/login" className="text-blue-600 hover:underline">
                {t('register.loginLink')}
              </Link>
            </p>
          </form>
        ) : (
          <form onSubmit={handleRegister} className="space-y-4">
            <p className="text-xs text-slate-500 bg-slate-50 rounded-lg px-3 py-2">
              {t('register.otpSentTo')} <span className="font-medium text-slate-700">{email}</span>
            </p>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('register.otp')}
              </label>
              <input
                type="text"
                required
                maxLength={8}
                value={otp}
                onChange={(e) => setOtp(e.target.value.toUpperCase())}
                placeholder={t('register.otpPlaceholder')}
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm font-mono tracking-widest focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('register.firstName')}
                </label>
                <input
                  type="text"
                  required
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder={t('register.firstNamePlaceholder')}
                  className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">
                  {t('register.lastName')}
                </label>
                <input
                  type="text"
                  required
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder={t('register.lastNamePlaceholder')}
                  className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('register.password')}
              </label>
              <input
                type="password"
                required
                minLength={8}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={t('register.passwordPlaceholder')}
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('register.confirmPassword')}
              </label>
              <input
                type="password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder={t('register.confirmPasswordPlaceholder')}
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('register.language')}
              </label>
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

            {error && <p className="text-red-500 text-xs">{error}</p>}

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 text-white font-medium py-2.5 rounded-xl transition-colors"
            >
              {loading ? t('register.submitting') : t('register.submit')}
            </button>

            <button
              type="button"
              onClick={() => { setStep(1); setError(''); }}
              className="w-full text-sm text-slate-500 hover:text-slate-700"
            >
              {t('register.backToStep1')}
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
