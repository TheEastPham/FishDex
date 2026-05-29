import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { revokeToken, buildLogoutUrl } from '../lib/auth/oidc';

export function useLogout() {
  const navigate = useNavigate();

  return async function logout() {
    const { accessToken, getRefreshToken, clearTokens } = useAuthStore.getState();
    const rt = getRefreshToken();

    clearTokens();

    // Fire-and-forget — don't block logout on network errors
    if (rt) revokeToken(rt).catch(() => {});
    if (accessToken) revokeToken(accessToken).catch(() => {});

    // Redirect through OpenIddict logout to clear server-side session
    window.location.href = buildLogoutUrl();

    // Fallback: if redirect fails for some reason, go to /login
    setTimeout(() => navigate('/login', { replace: true }), 3000);
  };
}
