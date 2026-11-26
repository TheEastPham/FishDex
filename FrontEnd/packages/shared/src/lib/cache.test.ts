import { afterEach, describe, expect, it, vi } from 'vitest';
import { CacheKeys, clearCache, getCached, invalidateCache, setCached } from './cache';

afterEach(() => {
  clearCache();
  vi.useRealTimers();
});

describe('cache', () => {
  it('returns a value until its TTL passes, then forgets it', () => {
    vi.useFakeTimers();
    setCached('k', { n: 1 }, 1000);

    vi.advanceTimersByTime(1000);
    expect(getCached('k')).toEqual({ n: 1 });

    vi.advanceTimersByTime(1);
    expect(getCached('k')).toBeNull();
  });

  it('invalidates a key and everything under it, but not keys that only share the start', () => {
    setCached('favorites', 1);
    setCached('favorites:mine', 2);
    setCached(CacheKeys.favoriteCheck(42), 3); // favorites:check:42
    setCached('favoritesX', 4);

    invalidateCache('favorites');

    expect(getCached('favorites')).toBeNull();
    expect(getCached('favorites:mine')).toBeNull();
    expect(getCached(CacheKeys.favoriteCheck(42))).toBeNull();
    expect(getCached('favoritesX')).toBe(4);
  });

  it('keeps species entries apart by language', () => {
    setCached(CacheKeys.speciesDetail(1, 'vi'), 'vi');
    setCached(CacheKeys.speciesDetail(1, 'en'), 'en');

    expect(getCached(CacheKeys.speciesDetail(1, 'vi'))).toBe('vi');
    expect(getCached(CacheKeys.speciesDetail(1, 'en'))).toBe('en');
  });
});
