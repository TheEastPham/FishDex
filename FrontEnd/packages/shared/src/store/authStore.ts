import { create } from 'zustand';

function parseJwt(token: string): Record<string, unknown> {
  try {
    const payload = token.split('.')[1];
    return JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')));
  } catch {
    return {};
  }
}

function extractRoles(claims: Record<string, unknown>): string[] {
  // OpenIddict emits "role" (singular); handle both string and array
  const raw = claims['role'] ?? claims['roles'];
  if (!raw) return [];
  if (Array.isArray(raw)) return raw.map(String);
  if (typeof raw === 'string') return [raw];
  return [];
}

interface AuthState {
  accessToken: string | null;
  isAuthenticated: boolean;
  isInitializing: boolean;
  roles: string[];
  userName: string | null;
  userEmail: string | null;
  setInitializing: (val: boolean) => void;
  setTokens: (access: string, refresh?: string) => void;
  clearTokens: () => void;
  getRefreshToken: () => string | null;
  hasRole: (role: string) => boolean;
}

// Access token: memory only (XSS-safe, lost on page reload → re-login via refresh token)
// Refresh token: sessionStorage (pragmatic for dev; upgrade to BFF httpOnly cookie for prod)
export const useAuthStore = create<AuthState>()((_set, get) => ({
  accessToken: null,
  isAuthenticated: false,
  isInitializing: true,
  roles: [],
  userName: null,
  userEmail: null,

  setInitializing: (val) => {
    useAuthStore.setState({ isInitializing: val });
  },

  setTokens: (access, refresh) => {
    if (refresh) sessionStorage.setItem('_rt', refresh);
    const claims = parseJwt(access);
    useAuthStore.setState({
      accessToken: access,
      isAuthenticated: true,
      roles: extractRoles(claims),
      userName: (claims['name'] as string) ?? null,
      userEmail: (claims['email'] as string) ?? null,
    });
  },

  clearTokens: () => {
    sessionStorage.removeItem('_rt');
    useAuthStore.setState({
      accessToken: null,
      isAuthenticated: false,
      roles: [],
      userName: null,
      userEmail: null,
    });
  },

  getRefreshToken: () => sessionStorage.getItem('_rt'),

  hasRole: (role: string): boolean => get().roles.includes(role),
}));
