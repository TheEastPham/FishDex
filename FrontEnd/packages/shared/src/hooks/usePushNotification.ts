import { useCallback, useEffect, useState } from 'react';
import { getVapidPublicKey, saveSubscription, removeSubscription } from '../lib/api/push';

type PermissionState = 'default' | 'granted' | 'denied' | 'unsupported';

function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = atob(base64);
  return Uint8Array.from([...rawData].map((c) => c.charCodeAt(0)));
}

export function usePushNotification() {
  const [permission, setPermission] = useState<PermissionState>('default');
  const [loading, setLoading] = useState(false);

  const isSupported =
    typeof window !== 'undefined' &&
    'serviceWorker' in navigator &&
    'PushManager' in window &&
    'Notification' in window;

  useEffect(() => {
    if (!isSupported) {
      setPermission('unsupported');
      return;
    }
    setPermission(Notification.permission as PermissionState);
  }, [isSupported]);

  const subscribe = useCallback(async () => {
    if (!isSupported) return;
    setLoading(true);
    try {
      const result = await Notification.requestPermission();
      setPermission(result as PermissionState);
      if (result !== 'granted') return;

      const reg = await navigator.serviceWorker.register('/push-sw.js');
      await navigator.serviceWorker.ready;

      const vapidKey = await getVapidPublicKey();
      const sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidKey),
      });

      const json = sub.toJSON();
      await saveSubscription({
        endpoint: sub.endpoint,
        p256dh: json.keys?.p256dh ?? '',
        auth: json.keys?.auth ?? '',
        userAgent: navigator.userAgent.substring(0, 200),
      });
    } catch (err) {
      console.error('[Push] subscribe failed', err);
    } finally {
      setLoading(false);
    }
  }, [isSupported]);

  const unsubscribe = useCallback(async () => {
    if (!isSupported) return;
    setLoading(true);
    try {
      const reg = await navigator.serviceWorker.getRegistration('/push-sw.js');
      const sub = await reg?.pushManager.getSubscription();
      if (sub) {
        await removeSubscription(sub.endpoint);
        await sub.unsubscribe();
      }
      setPermission('default');
    } catch (err) {
      console.error('[Push] unsubscribe failed', err);
    } finally {
      setLoading(false);
    }
  }, [isSupported]);

  return { permission, loading, isSupported, subscribe, unsubscribe };
}
