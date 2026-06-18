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

export interface UserProfileDto {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  fullName: string;
  avatar: string | null;
  language: string | null;
  phoneNumber?: string | null;
  roles: string[];
}

export interface UpdateProfilePayload {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  language?: string;
}

export interface ChangePasswordPayload {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export async function getMyProfile(): Promise<UserProfileDto> {
  const { data } = await apiClient.get<UserProfileDto>('/user/v1/account/profile');
  return data;
}

export async function updateMyProfile(payload: UpdateProfilePayload): Promise<UserProfileDto> {
  const { data } = await apiClient.put<UserProfileDto>('/user/v1/account/profile', payload);
  return data;
}

export async function changePassword(payload: ChangePasswordPayload): Promise<void> {
  await apiClient.put('/user/v1/account/password', payload);
}

export async function forgotPassword(email: string): Promise<ForgotPasswordResponse> {
  const { data } = await apiClient.post<ForgotPasswordResponse>('/user/v1/auth/forgot-password', { email });
  return data;
}

export async function resetPassword(payload: ResetPasswordPayload): Promise<ResetPasswordResponse> {
  const { data } = await apiClient.post<ResetPasswordResponse>('/user/v1/auth/reset-password', payload);
  return data;
}
