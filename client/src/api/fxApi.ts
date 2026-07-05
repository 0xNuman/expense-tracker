import { halFetch } from '../hal/api';

export interface RateQuote {
  rate: number;
  fetchedAtUtc: string;
  source: string;
}

export interface FxRatesResponse {
  base: string;
  asOf: string;
  rates: Record<string, RateQuote>;
}

export const fxApi = {
  getRates: async (token: string, baseCurrency: string = 'USD', asOf?: string): Promise<FxRatesResponse> => {
    const params = new URLSearchParams({ base: baseCurrency });
    if (asOf) {
      params.append('asOf', asOf);
    }
    return halFetch(`/api/fx/rates?${params.toString()}`, { method: 'GET' }, token);
  },

  createSnapshot: async (token: string, data: { from: string; to: string; rate: number; asOfUtc?: string }) => {
    return halFetch('/api/fx/snapshot', { method: 'POST', body: JSON.stringify(data) }, token);
  }
};
