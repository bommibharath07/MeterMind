import { Injectable, signal } from '@angular/core';
@Injectable({ providedIn: 'root' })
export class Api {
  token = signal(sessionStorage.getItem('metermind-token') || '');
  async connectDemo() {
    const data = await this.request<{ token: string }>('/auth/demo', { method: 'POST' });
    this.setToken(data.token);
  }
  setToken(value: string) {
    this.token.set(value);
    sessionStorage.setItem('metermind-token', value);
  }
  async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const response = await fetch(path, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...(this.token() ? { Authorization: `Bearer ${this.token()}` } : {}),
        ...options.headers,
      },
    });
    if (!response.ok) {
      let detail = '';
      try {
        const error = await response.json();
        detail = error.title || error.detail || '';
      } catch {}
      throw new Error(
        detail || `Request failed (${response.status}). Check API connectivity and sign-in.`,
      );
    }
    return response.json() as Promise<T>;
  }
  get<T>(path: string) {
    return this.request<T>('/api' + path);
  }
  post<T>(path: string, body: unknown = {}) {
    return this.request<T>('/api' + path, { method: 'POST', body: JSON.stringify(body) });
  }
  async download(id: string, format: string) {
    const res = await fetch(`/api/reports/${id}/download`, {
      headers: { Authorization: `Bearer ${this.token()}` },
    });
    if (!res.ok) throw new Error('Report is not ready or has expired.');
    const url = URL.createObjectURL(await res.blob());
    const a = document.createElement('a');
    a.href = url;
    a.download = `metermind-${id}.${format}`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
