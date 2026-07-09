import { useState } from 'react';
import { Link } from 'react-router-dom';
import { forgotPassword, useTranslation } from '@fishlover/shared';

export default function ForgotPasswordPage() {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    try {
      await forgotPassword(email);
      setSent(true);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-8 w-full max-w-sm">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-800">AquaHome 🐠</h1>
          <p className="text-slate-500 text-sm mt-1">{t('forgotPassword.subtitle')}</p>
        </div>

        {sent ? (
          <div className="space-y-4">
            <div className="bg-green-50 border border-green-200 rounded-xl p-4">
              <p className="font-medium text-green-800">{t('forgotPassword.successTitle')}</p>
              <p className="text-green-700 text-sm mt-1">{t('forgotPassword.successMessage')}</p>
            </div>
            <Link to="/login" className="block text-center text-sm text-blue-600 hover:underline">
              {t('forgotPassword.backToLogin')}
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                {t('forgotPassword.email')}
              </label>
              <input
                type="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="example@gmail.com"
                className="w-full border border-slate-200 rounded-xl px-3 py-2 text-base sm:text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 text-white font-medium py-2.5 rounded-xl transition-colors"
            >
              {loading ? t('forgotPassword.submitting') : t('forgotPassword.submit')}
            </button>

            <Link to="/login" className="block text-center text-sm text-slate-500 hover:text-slate-700">
              {t('forgotPassword.backToLogin')}
            </Link>
          </form>
        )}
      </div>
    </div>
  );
}
