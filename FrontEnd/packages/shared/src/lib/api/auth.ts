import { apiClient } from './client';

export interface RequestOtpResponse {
  success: boolean;
  message: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  verificationCode: string;
  phoneNumber?: string;
  language: string;
}

export interface RegisterResponse {
  success: boolean;
  message: string;
  userId?: string;
}

export async function requestOtp(email: string, invitationCode: string): Promise<RequestOtpResponse> {
  const { data } = await apiClient.get<RequestOtpResponse>(
    `/user/v1/auth/valid-email/${encodeURIComponent(email)}`,
    { params: { invitationCode } }
  );
  return data;
}

export async function registerUser(req: RegisterRequest): Promise<RegisterResponse> {
  const { data } = await apiClient.post<RegisterResponse>('/user/v1/auth/register', req);
  return data;
}
