import { describe, it, expect } from 'vitest';
import { Api } from './api';
describe('API client', () => {
  it('preserves missing measurements returned by the backend', async () => {
    globalThis.fetch=async()=>new Response('{"rssi":null,"pdr":0}',{status:200});
    const value=await new Api().get<{rssi:number|null;pdr:number}>('/meters/MM-0001');
    expect(value.rssi).toBeNull();expect(value.pdr).toBe(0);
  });
  it('rejects failed API requests with server problem details', async () => {
    globalThis.fetch = async () =>
      new Response(JSON.stringify({ title: 'Utility access denied' }), { status: 403 });
    const api = new Api();
    await expect(api.get('/meters')).rejects.toThrow('Utility access denied');
  });
  it('sends a scoped bearer credential and preserves query values', async () => {
    let captured: RequestInit | undefined;
    let path = '';
    globalThis.fetch = async (input, options) => {
      path = String(input);
      captured = options;
      return new Response('{"items":[],"total":0}', { status: 200 });
    };
    const api = new Api();
    api.setToken('test-access-token');
    const result = await api.get<{ total: number }>('/meters?RssiBelow=-90');
    expect(path).toContain('RssiBelow=-90');
    expect((captured!.headers as Record<string, string>)['Authorization']).toBe(
      'Bearer test-access-token',
    );
    expect(result.total).toBe(0);
  });
});
