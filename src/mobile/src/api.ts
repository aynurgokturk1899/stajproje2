import * as SecureStore from 'expo-secure-store';

declare const process: { env: { EXPO_PUBLIC_API_URL?: string } };

const API_URL = process.env.EXPO_PUBLIC_API_URL ?? 'http://10.0.2.2:5073';
const TOKEN_KEY = 'team-task-manager-token';

export const saveToken = (token: string) => SecureStore.setItemAsync(TOKEN_KEY, token);
export const clearToken = () => SecureStore.deleteItemAsync(TOKEN_KEY);
export const getToken = () => SecureStore.getItemAsync(TOKEN_KEY);

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = await getToken();
  try {
    const response = await fetch(`${API_URL}${path}`, {
      ...init,
      headers: { ...(init.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...init.headers }
    });
    const raw = response.status === 204 ? '' : await response.text();
    const body = raw ? JSON.parse(raw) : null;
    if (!response.ok) throw new Error(body?.errors?.[0] ?? body?.message ?? `Sunucu hatası (${response.status})`);
    return body?.data as T;
  } catch (error) {
    if (error instanceof TypeError) throw new Error('API bağlantısı kurulamadı. İnternet ve API adresini kontrol edin.');
    throw error;
  }
}

export async function uploadTaskFile(taskId: number, asset: { uri: string; name: string; mimeType?: string }): Promise<void> {
  const form = new FormData();
  form.append('file', { uri: asset.uri, name: asset.name, type: asset.mimeType ?? 'application/octet-stream' } as unknown as Blob);
  await api(`/api/tasks/${taskId}/attachments`, { method: 'POST', body: form });
}
