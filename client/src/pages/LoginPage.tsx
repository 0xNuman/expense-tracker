import { useState } from 'react';
import { requestMagicLink, beginPasskeyAuth, completePasskeyAuth } from '../hal/api';
import { useAuth } from '../auth/AuthContext';
import { useNavigate } from 'react-router-dom';
import { ErrorBanner } from '../components/ErrorBanner';
import { Spinner } from '../components/Spinner';

// WebAuthn needs base64url <-> ArrayBuffer helpers not provided by the DOM.
function base64UrlToBuffer(b64url: string): ArrayBuffer {
  const pad = '='.repeat((4 - (b64url.length % 4)) % 4);
  const b64 = (b64url + pad).replace(/-/g, '+').replace(/_/g, '/');
  const bin = atob(b64);
  const buf = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i);
  return buf.buffer;
}

function bufferToBase64Url(buf: ArrayBuffer | Uint8Array): string {
  const bytes = buf instanceof Uint8Array ? buf : new Uint8Array(buf);
  let bin = '';
  for (const b of bytes) bin += String.fromCharCode(b);
  return btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submitting) return;
    setError(null);
    setSubmitting(true);
    try {
      await requestMagicLink(email.trim());
      setSent(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not send magic link');
    } finally {
      setSubmitting(false);
    }
  };

  const handlePasskey = async () => {
    setError(null);
    try {
      if (!window.PublicKeyCredential) {
        setError('Passkeys are not supported in this browser.');
        return;
      }
      const { sessionId, options } = await beginPasskeyAuth(email.trim() || undefined);
      const opts = options as Record<string, unknown>;
      const publicKey = {
        ...opts,
        challenge: base64UrlToBuffer((opts.challenge as string) ?? ''),
        allowCredentials: Array.isArray(opts.allowCredentials)
          ? (opts.allowCredentials as Array<Record<string, unknown>>).map((c) => ({
              id: base64UrlToBuffer((c.id as string) ?? ''),
              type: c.type,
              transports: c.transports,
            }))
          : [],
      } as PublicKeyCredentialRequestOptions;
      const credential = (await navigator.credentials.get({ publicKey })) as PublicKeyCredential | null;
      if (!credential) return;
      const response = credential.response as AuthenticatorAssertionResponse;
      const assertion = {
        id: credential.id,
        rawId: credential.id,
        type: credential.type,
        response: {
          authenticatorData: bufferToBase64Url(response.authenticatorData),
          clientDataJSON: bufferToBase64Url(response.clientDataJSON),
          signature: bufferToBase64Url(response.signature),
          userHandle: response.userHandle ? bufferToBase64Url(response.userHandle) : null,
        },
      };
      const result = await completePasskeyAuth(sessionId, assertion);
      login(result);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Passkey sign-in failed');
    }
  };

  return (
    <div className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-50 dark:bg-slate-950">
      {/* Decorative background blobs for glassmorphism */}
      <div className="absolute top-[-10%] left-[-10%] h-[500px] w-[500px] rounded-full bg-sky-400/20 blur-[100px] dark:bg-sky-600/20" />
      <div className="absolute bottom-[-10%] right-[-10%] h-[500px] w-[500px] rounded-full bg-indigo-400/20 blur-[100px] dark:bg-indigo-600/20" />
      
      <div className="relative z-10 w-full max-w-sm">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-semibold tracking-tight">Expense Tracker</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Sign in to track your spending</p>
        </div>
        <div className="glass rounded-2xl p-8 shadow-2xl">
          {sent ? (
            <div className="text-center">
              <p className="font-medium">Check your email</p>
              <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
                We sent a sign-in link to <span className="font-medium">{email}</span>. It expires in 15 minutes.
              </p>
              <button
                type="button"
                onClick={() => setSent(false)}
                className="mt-4 text-sm font-medium text-sky-600 hover:text-sky-700 dark:text-sky-400"
              >
                Use a different email
              </button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}
              <label className="flex flex-col gap-1 text-sm">
                <span className="font-medium text-slate-700 dark:text-slate-300">Email</span>
                <input
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  className="input"
                  autoFocus
                />
              </label>
              <button
                type="submit"
                disabled={submitting}
                className="inline-flex items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-sky-500 to-indigo-600 px-4 py-3 text-white font-medium transition-all hover:scale-[1.02] hover:shadow-lg disabled:opacity-60 disabled:hover:scale-100"
              >
                {submitting && <Spinner />}
                Send magic link
              </button>
              <div className="relative my-2 text-center">
                <span className="text-xs text-slate-400">or</span>
              </div>
              <button
                type="button"
                onClick={handlePasskey}
                className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white/50 px-4 py-3 text-sm font-medium text-slate-700 transition-all hover:bg-slate-100 hover:scale-[1.02] dark:border-slate-700 dark:bg-slate-800/50 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                🔑 Sign in with passkey
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}