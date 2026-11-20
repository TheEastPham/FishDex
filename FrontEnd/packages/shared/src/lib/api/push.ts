import { apiClient } from './client';

export interface SaveSubscriptionPayload {
  endpoint: string;
  p256dh: string;
  auth: string;
  userAgent?: string;
}

export async function getVapidPublicKey(): Promise<string> {
  const { data } = await apiClient.get<{ publicKey: string }>('/user/v1/push/vapid-public-key');
  return data.publicKey;
}

export async function saveSubscription(payload: SaveSubscriptionPayload): Promise<void> {
  await apiClient.post('/user/v1/push/subscribe', payload);
}

export async function removeSubscription(endpoint: string): Promise<void> {
  await apiClient.delete('/user/v1/push/unsubscribe', { data: { endpoint } });
}
