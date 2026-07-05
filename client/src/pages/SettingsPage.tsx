import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { Layout } from '../components/Layout';
import { ErrorBanner } from '../components/ErrorBanner';
import { Spinner } from '../components/Spinner';
import { beginPasskeyRegistration, completePasskeyRegistration } from '../hal/api';

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

export function SettingsPage() {
  const { accessToken, user } = useAuth();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleAddPasskey = async () => {
    if (!accessToken) return;
    setError(null);
    setSuccess(null);
    setBusy(true);
    try {
      if (!window.PublicKeyCredential) {
        setError('Passkeys are not supported in this browser.');
        return;
      }
      const options = (await beginPasskeyRegistration(accessToken, 'Passkey')) as Record<string, unknown>;
      const excludeCredentials = Array.isArray((options.publicKey as Record<string, unknown> | undefined)?.excludeCredentials)
        ? (((options.publicKey as Record<string, unknown>).excludeCredentials as Array<Record<string, unknown>>).map((c) => ({
            id: base64UrlToBuffer((c.id as string) ?? ''),
            type: c.type,
            transports: c.transports,
          })))
        : [];
      const publicKey = {
        ...((options.publicKey as Record<string, unknown> | undefined) ?? options),
        challenge: base64UrlToBuffer(((options.publicKey as Record<string, unknown> | undefined)?.challenge as string) ?? (options.challenge as string) ?? ''),
        user: (() => {
          const u = ((options.publicKey as Record<string, unknown> | undefined)?.user ?? options.user) as Record<string, unknown> | undefined;
          if (!u) return undefined;
          return { ...u, id: base64UrlToBuffer((u.id as string) ?? '') };
        })(),
        excludeCredentials,
      } as PublicKeyCredentialCreationOptions;
      const credential = (await navigator.credentials.create({ publicKey })) as PublicKeyCredential | null;
      if (!credential) return;
      const response = credential.response as AuthenticatorAttestationResponse;
      const attestation = {
        id: credential.id,
        rawId: credential.id,
        type: credential.type,
        response: {
          attestationObject: bufferToBase64Url(response.attestationObject),
          clientDataJSON: bufferToBase64Url(response.clientDataJSON),
        },
      };
      await completePasskeyRegistration(accessToken, attestation, 'Passkey');
      setSuccess('Passkey registered. You can now use it to sign in.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not register passkey');
    } finally {
      setBusy(false);
    }
  };

  return (
    <Layout>
      <div className="mx-auto max-w-2xl">
        <h2 className="text-lg font-semibold">Settings</h2>

        <section className="mt-4 rounded-xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
          <h3 className="font-medium">Account</h3>
          <dl className="mt-2 grid grid-cols-2 gap-2 text-sm">
            <dt className="text-slate-500 dark:text-slate-400">Email</dt>
            <dd>{user.email ?? '—'}</dd>
            <dt className="text-slate-500 dark:text-slate-400">Tenant</dt>
            <dd>{user.tenantName ?? '—'}</dd>
          </dl>
        </section>

        <section className="mt-4 rounded-xl border border-slate-200 bg-white p-5 dark:border-slate-800 dark:bg-slate-900">
          <h3 className="font-medium">Security</h3>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Register a passkey to sign in faster on this device.
          </p>
          {error && (
            <div className="mt-3">
              <ErrorBanner message={error} onDismiss={() => setError(null)} />
            </div>
          )}
          {success && (
            <div className="mt-3 rounded-lg border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-300">
              {success}
            </div>
          )}
          <button
            type="button"
            onClick={handleAddPasskey}
            disabled={busy}
            className="mt-3 inline-flex items-center gap-2 rounded-lg bg-sky-600 px-4 py-2 text-white font-medium hover:bg-sky-700 disabled:opacity-60"
          >
            {busy && <Spinner />}
            Add passkey
          </button>
        </section>
      </div>
    </Layout>
  );
}