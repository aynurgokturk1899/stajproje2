// @ts-nocheck
const apiUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:5073';

export async function request(path, options = {}) {
  const token = localStorage.getItem('accessToken');
  const response = await fetch(`${apiUrl}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers
    }
  });

  const rawBody = response.status === 204 ? '' : await response.text();
  let body = null;
  if (rawBody) {
    try {
      body = JSON.parse(rawBody);
    } catch {
      throw new Error('Sunucudan beklenmeyen bir cevap alındı. API’nin güncel sürümle çalıştığını kontrol edin.');
    }
  }
  if (response.status === 401 && token) {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('session');
    window.dispatchEvent(new CustomEvent('session-expired'));
    throw new Error('Oturum süreniz doldu. Lütfen yeniden giriş yapın.');
  }
  if (!response.ok) throw new Error(body?.errors?.[0] ?? body?.message ?? `İşlem gerçekleştirilemedi (HTTP ${response.status}).`);
  return body?.data;
}

export const login = (email, password) => request('/api/auth/login', {
  method: 'POST',
  body: JSON.stringify({ email, password })
});

export const changePassword = (currentPassword, newPassword) => request('/api/auth/change-password', {
  method: 'POST',
  body: JSON.stringify({ currentPassword, newPassword })
});

export const register = (payload) => request('/api/auth/register', {
  method: 'POST',
  body: JSON.stringify(payload)
});

export async function uploadAttachment(taskId, file) {
  const form = new FormData();
  form.append('file', file);
  const response = await fetch(`${apiUrl}/api/tasks/${taskId}/attachments`, { method: 'POST', headers: { Authorization: `Bearer ${localStorage.getItem('accessToken')}` }, body: form });
  const body = await response.json();
  if (!response.ok) throw new Error(body?.errors?.[0] ?? body?.message ?? 'Dosya yüklenemedi.');
  return body.data;
}
