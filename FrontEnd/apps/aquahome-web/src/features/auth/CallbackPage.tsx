import { useEffect, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore, exchangeCode } from '@fishlover/shared';

export default function CallbackPage() {
  const { setTokens } = useAuthStore();
  const navigate = useNavigate();
  const handled = useRef(false);

  useEffect(() => {
    // StrictMode double-invoke guard
    if (handled.current) return;
    handled.current = true;

    const params = new URLSearchParams(window.location.search);
    const code = params.get('code');
    const returnedState = params.get('state');
    const error = params.get('error');

    if (error) {
      console.error('OIDC error:', error, params.get('error_description'));
      navigate('/login', { replace: true });
      return;
    }

    const verifier = sessionStorage.getItem('pkce_verifier');
    const storedState = sessionStorage.getItem('pkce_state');

    // Validate state (CSRF protection) and verifier presence
    if (!code || !verifier || returnedState !== storedState) {
      navigate('/login', { replace: true });
      return;
    }

    sessionStorage.removeItem('pkce_verifier');
    sessionStorage.removeItem('pkce_state');

    exchangeCode(code, verifier)
      .then((tokens) => {
        setTokens(tokens.access_token, tokens.refresh_token);
        navigate('/dashboard', { replace: true });
      })
      .catch(() => navigate('/login', { replace: true }));
  }, [navigate, setTokens]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <p className="text-slate-400 text-sm animate-pulse">Đang xử lý đăng nhập…</p>
    </div>
  );
}
