import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const post = vi.fn();
vi.mock('axios', () => ({ default: { create: () => ({ post }) } }));

const NOW = new Date('2026-11-26T12:00:00Z').getTime();
const inMinutes = (m: number) => new Date(NOW + m * 60_000).toISOString();

// Module giữ SAS ở biến cấp module → mỗi test nạp lại bản mới.
async function load() {
  vi.resetModules();
  return import('./mediaAccess');
}

beforeEach(() => {
  vi.useFakeTimers();
  vi.setSystemTime(NOW);
  post.mockReset();
});

afterEach(() => vi.useRealTimers());

describe('media access SAS', () => {
  it('asks the gateway once however many images want a SAS at the same time', async () => {
    const m = await load();
    post.mockResolvedValue({ data: { sas: 'sig=1', expiresAt: inMinutes(60) } });

    await Promise.all([m.ensureMediaSas(), m.ensureMediaSas(), m.ensureMediaSas()]);

    expect(post).toHaveBeenCalledOnce();
    expect(post).toHaveBeenCalledWith('/fishdex/v1/public/media-access/sas');
    expect(m.getMediaSasState()).toEqual({ status: 'ready', sas: 'sig=1' });
  });

  it('renews five minutes before expiry and keeps showing images meanwhile', async () => {
    const m = await load();
    post.mockResolvedValueOnce({ data: { sas: 'sig=1', expiresAt: inMinutes(60) } });
    await m.ensureMediaSas();

    let resolveRenewal!: (v: unknown) => void;
    post.mockReturnValueOnce(new Promise((r) => { resolveRenewal = r; }));
    vi.advanceTimersByTime(55 * 60_000);

    expect(post).toHaveBeenCalledTimes(2);
    expect(m.getMediaSasState()).toEqual({ status: 'ready', sas: 'sig=1' }); // không nháy sang loading

    resolveRenewal({ data: { sas: 'sig=2', expiresAt: inMinutes(120) } });
    await vi.waitFor(() => expect(m.getMediaSasState().sas).toBe('sig=2'));
  });

  it('waits a minute before asking again after a failure', async () => {
    const m = await load();
    post.mockRejectedValue(new Error('503'));

    await m.ensureMediaSas();
    expect(m.getMediaSasState().status).toBe('failed');

    vi.advanceTimersByTime(59_000);
    await m.ensureMediaSas();
    expect(post).toHaveBeenCalledOnce();

    vi.advanceTimersByTime(2_000);
    await m.ensureMediaSas();
    expect(post).toHaveBeenCalledTimes(2);
  });

  it('notifies subscribers until they unsubscribe', async () => {
    const m = await load();
    post.mockResolvedValue({ data: { sas: 'sig=1', expiresAt: inMinutes(60) } });
    const listener = vi.fn();
    const unsubscribe = m.subscribeMediaSas(listener);

    await m.ensureMediaSas();
    const calls = listener.mock.calls.length;
    unsubscribe();
    vi.advanceTimersByTime(55 * 60_000);
    await vi.waitFor(() => expect(post).toHaveBeenCalledTimes(2));

    expect(calls).toBeGreaterThan(0);
    expect(listener).toHaveBeenCalledTimes(calls);
  });

  it('builds a stable image URL with the key encoded', async () => {
    const m = await load();

    expect(m.buildMediaUrl('123/a b.jpg', 'sv=1&sig=x')).toBe(
      'http://gateway.test/fishdex/v1/public/media-access?path=123%2Fa%20b.jpg&sv=1&sig=x',
    );
  });
});
