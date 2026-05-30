import { useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { refreshAccessToken } from '../lib/auth/oidc';

let restorePromise: Promise<void> | null = null;

export function useAuthRestore() {
  const { setTokens, clearTokens, getRefreshToken, isInitializing, setInitializing } = useAuthStore();

  useEffect(() => {
    // Only run this restore logic once on mount
    const rt = getRefreshToken();
    if (!rt) {
      setInitializing(false);
      return;
    }

    if (!restorePromise) {
      restorePromise = refreshAccessToken(rt)
        .then((tokens) => {
          setTokens(tokens.access_token, tokens.refresh_token);
        })
        .catch((error) => {
          console.error('Failed to restore session on reload', error);
          clearTokens();
        })
        .finally(() => {
          setInitializing(false);
          // Don't reset restorePromise here if we want to protect against strict mode double-mounts
          // But it's okay to reset it after a delay, or just leave it since the app only mounts once per reload
        });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return isInitializing;
}
