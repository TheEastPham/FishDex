import axios from 'axios';
import { useAuthStore } from '../../store/authStore';
import { refreshAccessToken } from '../auth/oidc';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_URL,
  timeout: 10000, // 10s timeout — never hang indefinitely
});

// ── Request interceptor: attach Bearer token ───────────────
apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Deduplicates concurrent refresh calls — only one in-flight refresh at a time
let refreshing: Promise<string> | null = null;

// ── Response interceptor: handle errors centrally ─────────
apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    const url = error.config?.url ?? '(unknown)';
    const status: number | undefined = error.response?.status;

    if (!error.response) {
      if (import.meta.env.DEV) console.warn(`[API] Network error on ${url}:`, error.message);
      return Promise.reject(error);
    }

    if (import.meta.env.DEV) console.warn(`[API] ${status} on ${url}`, error.response?.data ?? '');

    // Only attempt token refresh for 401 — all other errors pass through
    if (status !== 401) {
      return Promise.reject(error);
    }

    const original = error.config;

    // If already retried after a refresh → session truly expired → go to login
    if (original._retry) {
      useAuthStore.getState().clearTokens();
      return Promise.reject(error);
    }
    original._retry = true;

    const { getRefreshToken, setTokens, clearTokens } = useAuthStore.getState();
    const rt = getRefreshToken();
    if (!rt) {
      clearTokens();
      return Promise.reject(error);
    }

    try {
      refreshing ??= refreshAccessToken(rt).then((tokens) => {
        setTokens(tokens.access_token, tokens.refresh_token);
        refreshing = null;
        return tokens.access_token;
      });

      const newToken = await refreshing;
      original.headers.Authorization = `Bearer ${newToken}`;
      return apiClient(original);
    } catch {
      clearTokens();
      return Promise.reject(error);
    }
  }
);
