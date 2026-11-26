import { createHash } from 'node:crypto';
import { describe, expect, it } from 'vitest';
import { generateCodeChallenge, generateCodeVerifier, generateState } from './pkce';

const BASE64URL = /^[A-Za-z0-9_-]+$/;

describe('PKCE', () => {
  it('derives the S256 challenge: base64url(SHA-256(verifier)), as the server recomputes it', async () => {
    const verifier = generateCodeVerifier();
    const expected = createHash('sha256').update(verifier).digest('base64url');

    expect(await generateCodeChallenge(verifier)).toBe(expected);
  });

  it('makes a 43-character base64url verifier, different each time', () => {
    const a = generateCodeVerifier();
    const b = generateCodeVerifier();

    expect(a).toMatch(BASE64URL);
    expect(a).toHaveLength(43); // 32 byte → 43 ký tự, nằm trong khoảng 43–128 RFC cho phép
    expect(a).not.toBe(b);
  });

  it('makes a base64url state without padding', () => {
    expect(generateState()).toMatch(BASE64URL);
  });
});
