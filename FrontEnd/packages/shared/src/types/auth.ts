export interface TokenResponse {
  access_token: string;
  refresh_token?: string;
  token_type: string;
  expires_in: number;
  scope?: string;
}

export interface UserInfo {
  sub: string;
  email?: string;
  name?: string;
  roles?: string[];
}
