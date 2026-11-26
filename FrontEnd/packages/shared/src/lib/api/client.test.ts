// @vitest-environment jsdom
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useAuthStore } from '../../store/authStore';
import { refreshAccessToken } from '../auth/oidc';
import { apiClient } from './client';

vi.mock('../auth/oidc', () => ({ refreshAccessToken: vi.fn() }));

/** JWT không chữ ký, đủ để authStore đọc claim. */
function jwt(name: string): string {
  const payload = btoa(JSON.stringify({ name })).replace(/=+$/, '');
  return `e30.${payload}.sig`;
}

type Handler = (config: InternalAxiosRequestConfig) => number;

/** Thay adapter HTTP: handler trả mã status cho từng request; ghi lại header Authorization đã gửi. */
function serve(handler: Handler) {
  const sent: (string | undefined)[] = [];
  apiClient.defaults.adapter = async (config) => {
    sent.push(config.headers.Authorization as string | undefined);
    const status = handler(config);
    const response = { data: { ok: status }, status, statusText: '', headers: {}, config } as AxiosResponse;
    if (status >= 400) throw new AxiosError(`HTTP ${status}`, 'ERR_BAD_REQUEST', config, null, response);
    return response;
  };
  return sent;
}

beforeEach(() => {
  sessionStorage.clear();
  useAuthStore.getState().clearTokens();
});

afterEach(() => {
  vi.mocked(refreshAccessToken).mockReset();
});

describe('apiClient', () => {
  it('sends the access token as a Bearer header', async () => {
    const access = jwt('An');
    useAuthStore.getState().setTokens(access, 'rt-1');
    const sent = serve(() => 200);

    await apiClient.get('/x');

    expect(sent).toEqual([`Bearer ${access}`]);
  });

  it('refreshes once on 401 and retries the request with the new token', async () => {
    useAuthStore.getState().setTokens(jwt('old'), 'rt-1');
    const fresh = jwt('new');
    vi.mocked(refreshAccessToken).mockResolvedValue({ access_token: fresh, refresh_token: 'rt-2' } as never);
    const sent = serve((c) => (c.headers.Authorization === `Bearer ${fresh}` ? 200 : 401));

    const res = await apiClient.get('/x');

    expect(res.status).toBe(200);
    expect(refreshAccessToken).toHaveBeenCalledWith('rt-1');
    expect(sent.at(-1)).toBe(`Bearer ${fresh}`);
    expect(sessionStorage.getItem('_rt')).toBe('rt-2');
  });

  it('shares one refresh between requests that fail at the same time', async () => {
    useAuthStore.getState().setTokens(jwt('old'), 'rt-1');
    const fresh = jwt('new');
    vi.mocked(refreshAccessToken).mockResolvedValue({ access_token: fresh, refresh_token: 'rt-2' } as never);
    serve((c) => (c.headers.Authorization === `Bearer ${fresh}` ? 200 : 401));

    await Promise.all([apiClient.get('/a'), apiClient.get('/b'), apiClient.get('/c')]);

    expect(refreshAccessToken).toHaveBeenCalledOnce();
  });

  it('signs the user out when there is no refresh token', async () => {
    useAuthStore.setState({ accessToken: jwt('old'), isAuthenticated: true });
    serve(() => 401);

    await expect(apiClient.get('/x')).rejects.toBeInstanceOf(AxiosError);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(refreshAccessToken).not.toHaveBeenCalled();
  });

  it('signs the user out when the refresh fails', async () => {
    useAuthStore.getState().setTokens(jwt('old'), 'rt-1');
    vi.mocked(refreshAccessToken).mockRejectedValue(new Error('invalid_grant'));
    serve(() => 401);

    await expect(apiClient.get('/x')).rejects.toBeInstanceOf(AxiosError);
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
    expect(sessionStorage.getItem('_rt')).toBeNull();
  });

  it('can refresh again after an earlier refresh failed', async () => {
    // Trước đây promise refresh lỗi bị giữ lại: lần đăng nhập sau, 401 đầu tiên làm user bị đăng xuất ngay.
    useAuthStore.getState().setTokens(jwt('old'), 'rt-1');
    vi.mocked(refreshAccessToken).mockRejectedValueOnce(new Error('network'));
    serve(() => 401);
    await expect(apiClient.get('/x')).rejects.toBeInstanceOf(AxiosError);

    useAuthStore.getState().setTokens(jwt('again'), 'rt-3');
    const fresh = jwt('new');
    vi.mocked(refreshAccessToken).mockResolvedValueOnce({ access_token: fresh, refresh_token: 'rt-4' } as never);
    serve((c) => (c.headers.Authorization === `Bearer ${fresh}` ? 200 : 401));

    await expect(apiClient.get('/x')).resolves.toMatchObject({ status: 200 });
    expect(refreshAccessToken).toHaveBeenLastCalledWith('rt-3');
  });

  it('gives up after one retry when the new token is refused too', async () => {
    useAuthStore.getState().setTokens(jwt('old'), 'rt-1');
    vi.mocked(refreshAccessToken).mockResolvedValue({ access_token: jwt('new'), refresh_token: 'rt-2' } as never);
    const sent = serve(() => 401);

    await expect(apiClient.get('/x')).rejects.toBeInstanceOf(AxiosError);
    expect(sent).toHaveLength(2);
    expect(refreshAccessToken).toHaveBeenCalledOnce();
    expect(useAuthStore.getState().isAuthenticated).toBe(false);
  });

  it('passes other errors through without touching the session', async () => {
    useAuthStore.getState().setTokens(jwt('An'), 'rt-1');
    serve(() => 422);

    await expect(apiClient.get('/x')).rejects.toMatchObject({ response: { status: 422 } });
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
    expect(refreshAccessToken).not.toHaveBeenCalled();
  });
});
