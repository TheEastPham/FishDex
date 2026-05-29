import axios from 'axios';
import { useAuthStore } from '../../store/authStore';
import { refreshAccessToken } from '../auth/oidc';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_AQUAHOME_API_URL,
});

apiClient.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Deduplicates concurrent refresh calls — only one in-flight refresh at a time
let refreshing: Promise<string> | null = null;

apiClient.interceptors.response.use(
  (res) => res,
  async (error) => {
    if (error.response?.status !== 401) return Promise.reject(error);

    const original = error.config;
    if (original._retry) {
      useAuthStore.getState().clearTokens();
      window.location.replace('/login');
      return Promise.reject(error);
    }
    original._retry = true;

    const { getRefreshToken, setTokens, clearTokens } = useAuthStore.getState();
    const rt = getRefreshToken();
    if (!rt) {
      clearTokens();
      window.location.replace('/login');
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
      window.location.replace('/login');
      return Promise.reject(error);
    }
  }
);
