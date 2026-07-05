import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { verifyMagicLink } from '../hal/api';
import { useAuth } from '../auth/AuthContext';
import { Spinner } from '../components/Spinner';
import { ErrorBanner } from '../components/ErrorBanner';

export function LoginCompletePage() {
  const [params] = useSearchParams();
  const token = params.get('token');
  const { login } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      if (!token) {
        setError('Missing sign-in token in the link.');
        return;
      }
      try {
        const result = await verifyMagicLink(token);
        if (cancelled) return;
        login(result);
        navigate('/', { replace: true });
      } catch (err) {
        if (cancelled) return;
        setError(err instanceof Error ? err.message : 'Sign-in failed. The link may have expired.');
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [token, login, navigate]);

  return (
    <div className="flex min-h-full items-center justify-center bg-slate-50 px-4 dark:bg-slate-950">
      <div className="w-full max-w-sm rounded-2xl border border-slate-200 bg-white p-6 text-center shadow-sm dark:border-slate-800 dark:bg-slate-900">
        {error ? (
          <div className="flex flex-col gap-4">
            <ErrorBanner message={error} />
            <button
              type="button"
              onClick={() => navigate('/login', { replace: true })}
              className="text-sm font-medium text-sky-600 hover:text-sky-700 dark:text-sky-400"
            >
              Back to sign in
            </button>
          </div>
        ) : (
          <div className="flex flex-col items-center gap-3">
            <Spinner className="h-8 w-8" />
            <p className="text-sm text-slate-600 dark:text-slate-300">Verifying your sign-in link…</p>
          </div>
        )}
      </div>
    </div>
  );
}