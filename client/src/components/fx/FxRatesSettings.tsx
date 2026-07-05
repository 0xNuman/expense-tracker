import React, { useState, useEffect } from 'react';
import { fxApi } from '../../api/fxApi';
import { useAuth } from '../../auth/AuthContext';
import type { FxRatesResponse, RateQuote } from '../../api/fxApi';

export const FxRatesSettings: React.FC = () => {
  const { accessToken: token } = useAuth();
  const [rates, setRates] = useState<FxRatesResponse | null>(null);
  const [baseCurrency, setBaseCurrency] = useState('USD');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchRates();
  }, [baseCurrency]);

  const fetchRates = async () => {
    try {
      setLoading(true);
      setError(null);
      if (!token) throw new Error('Not authenticated');
      const data = await fxApi.getRates(token, baseCurrency);
      setRates(data);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch rates');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-2">
      <h2 className="text-xl font-semibold mb-4">Exchange Rates</h2>
      
      <div className="mb-4">
        <label className="block text-sm font-medium mb-1">
          Base Currency
        </label>
        <select
          value={baseCurrency}
          onChange={(e) => setBaseCurrency(e.target.value)}
          className="mt-1 block w-full pl-3 pr-10 py-2 text-base border-slate-300 dark:border-slate-700 bg-transparent focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm rounded-md"
        >
          <option value="USD">USD - US Dollar</option>
          <option value="EUR">EUR - Euro</option>
          <option value="GBP">GBP - British Pound</option>
          <option value="INR">INR - Indian Rupee</option>
          <option value="JPY">JPY - Japanese Yen</option>
        </select>
      </div>

      {loading ? (
        <p className="text-slate-500 dark:text-slate-400">Loading rates...</p>
      ) : error ? (
        <p className="text-red-500">{error}</p>
      ) : rates ? (
        <div>
          <p className="text-sm text-slate-500 dark:text-slate-400 mb-4">
            Rates as of {new Date(rates.asOf).toLocaleString()}
          </p>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {Object.entries(rates.rates).map(([currency, data]) => {
              const quote = data as RateQuote;
              return (
              <div key={currency} className="border border-slate-200 dark:border-slate-700 rounded-lg p-4 bg-slate-50 dark:bg-slate-800">
                <div className="text-lg font-medium">{currency}</div>
                <div className="text-slate-900 dark:text-slate-100 mt-1">{quote.rate.toFixed(4)}</div>
                <div className="text-xs text-slate-500 dark:text-slate-400 mt-2">Source: {quote.source}</div>
              </div>
            )})}
          </div>
        </div>
      ) : null}
    </div>
  );
};
