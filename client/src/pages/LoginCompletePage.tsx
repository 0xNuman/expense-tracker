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
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-50 px-4 dark:bg-slate-950">
      {/* Decorative background blobs */}
      <div className="absolute top-[-10%] left-[-10%] h-[500px] w-[500px] rounded-full bg-sky-400/20 blur-[100px] dark:bg-sky-600/20" />
      <div className="absolute bottom-[-10%] right-[-10%] h-[500px] w-[500px] rounded-full bg-indigo-400/20 blur-[100px] dark:bg-indigo-600/20" />
      
      <div className="relative z-10 w-full max-w-sm glass rounded-2xl p-8 text-center shadow-2xl">
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