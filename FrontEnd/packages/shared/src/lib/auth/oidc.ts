import type { TokenResponse } from '../../types/auth';
import { generateCodeChallenge } from './pkce';

const authority = import.meta.env.VITE_AUTH_ISSUER;
const clientId = import.meta.env.VITE_AUTH_CLIENT_ID;
const redirectUri = import.meta.env.VITE_AUTH_REDIRECT_URI;
const postLogoutUri = import.meta.env.VITE_AUTH_POST_LOGOUT_URI;

const endpoints = {
  authorize: `${authority}/connect/authorize`,
  token:     `${authority}/connect/token`,
  revoke:    `${authority}/connect/revoke`,
  logout:    `${authority}/connect/logout`,
} as const;

const SCOPES = 'openid profile email roles';

export async function buildAuthorizeUrl(verifier: string, state: string): Promise<string> {
  const challenge = await generateCodeChallenge(verifier);
  const params = new URLSearchParams({
    response_type: 'code',
    client_id: clientId,
    redirect_uri: redirectUri,
    scope: SCOPES,
    state,
    code_challenge: challenge,
    code_challenge_method: 'S256',
  });
  return `${endpoints.authorize}?${params}`;
}

export async function exchangeCode(code: string, verifier: string): Promise<TokenResponse> {
  const res = await fetch(endpoints.token, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'authorization_code',
      code,
      redirect_uri: redirectUri,
      client_id: clientId,
      code_verifier: verifier,
    }),
  });
  if (!res.ok) throw new Error(`Token exchange failed: ${res.status}`);
  return res.json();
}

export async function refreshAccessToken(refreshToken: string): Promise<TokenResponse> {
  const res = await fetch(endpoints.token, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      refresh_token: refreshToken,
      client_id: clientId,
    }),
  });
  if (!res.ok) throw new Error(`Token refresh failed: ${res.status}`);
  return res.json();
}

export async function revokeToken(token: string): Promise<void> {
  await fetch(endpoints.revoke, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ token, client_id: clientId }),
  });
}

export function buildLogoutUrl(): string {
  const params = new URLSearchParams({ post_logout_redirect_uri: postLogoutUri });
  return `${endpoints.logout}?${params}`;
}
