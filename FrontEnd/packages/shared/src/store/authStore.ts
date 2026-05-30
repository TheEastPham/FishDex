import { create } from 'zustand';

interface AuthState {
  accessToken: string | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  setInitializing: (val: boolean) => void;
  setTokens: (access: string, refresh?: string) => void;
  clearTokens: () => void;
  getRefreshToken: () => string | null;
}

// Access token: memory only (XSS-safe, lost on page reload → re-login)
// Refresh token: sessionStorage (pragmatic for dev; upgrade to BFF httpOnly cookie for prod)
export const useAuthStore = create<AuthState>(() => ({
  accessToken: null,
  isAuthenticated: false,
  isInitializing: true,

  setInitializing: (val) => {
    useAuthStore.setState({ isInitializing: val });
  },

  setTokens: (access, refresh) => {
    if (refresh) sessionStorage.setItem('_rt', refresh);
    useAuthStore.setState({ accessToken: access, isAuthenticated: true });
  },

  clearTokens: () => {
    sessionStorage.removeItem('_rt');
    useAuthStore.setState({ accessToken: null, isAuthenticated: false });
  },

  getRefreshToken: () => sessionStorage.getItem('_rt'),
}));
