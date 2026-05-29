import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore, buildAuthorizeUrl, generateCodeVerifier, generateState, useTranslation } from '@fishlover/shared';

export default function LoginPage() {
  const { t } = useTranslation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) navigate('/dashboard', { replace: true });
  }, [isAuthenticated, navigate]);

  async function handleLogin() {
    const verifier = generateCodeVerifier();
    const state = generateState();
    sessionStorage.setItem('pkce_verifier', verifier);
    sessionStorage.setItem('pkce_state', state);
    window.location.href = await buildAuthorizeUrl(verifier, state);
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200 p-8 w-full max-w-sm">
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-800">AquaHome 🐠</h1>
          <p className="text-slate-500 text-sm mt-1">{t('login.subtitle')}</p>
        </div>
        <button
          onClick={handleLogin}
          className="w-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white font-medium py-2.5 rounded-xl transition-colors"
        >
          {t('login.button')}
        </button>
      </div>
    </div>
  );
}
