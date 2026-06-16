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

export interface ForgotPasswordResponse {
  message: string;
}

export interface ResetPasswordPayload {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ResetPasswordResponse {
  message: string;
}

export async function forgotPassword(email: string): Promise<ForgotPasswordResponse> {
  const { data } = await apiClient.post<ForgotPasswordResponse>('/user/v1/auth/forgot-password', { email });
  return data;
}

export async function resetPassword(payload: ResetPasswordPayload): Promise<ResetPasswordResponse> {
  const { data } = await apiClient.post<ResetPasswordResponse>('/user/v1/auth/reset-password', payload);
  return data;
}
