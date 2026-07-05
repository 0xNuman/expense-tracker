import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { refreshToken as refreshApi } from '../hal/api';
import type { AuthResult } from '../hal/api';
import { configureHalClient } from '../hal/HalClient';

export interface AuthUser {
  email: string | undefined;
  tenantName: string | undefined;
  tenantId: string | undefined;
}

interface AuthContextValue {
  accessToken: string | null;
  user: AuthUser;
  isAuthenticated: boolean;
  bootstrapped: boolean;
  login: (result: AuthResult) => void;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [user, setUser] = useState<AuthUser>({ email: undefined, tenantName: undefined, tenantId: undefined });
  const [bootstrapped, setBootstrapped] = useState(false);
  const loggingOut = useRef(false);

  // On mount, try to restore a session from the refresh cookie.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const result = await refreshApi();
        if (cancelled) return;
        if (result) {
          setAccessToken(result.accessToken);
          setUser({
            email: result.email,
            tenantName: result.tenantName,
            tenantId: result.tenantId,
          });
        }
      } catch {
        // network error or server down — stay logged out; routes will guard.
      } finally {
        if (!cancelled) setBootstrapped(true);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback((result: AuthResult) => {
    setAccessToken(result.accessToken);
    setUser({
      email: result.email,
      tenantName: result.tenantName,
      tenantId: result.tenantId,
    });
  }, []);

  const logout = useCallback(async () => {
    if (loggingOut.current) return;
    loggingOut.current = true;
    try {
      // Best-effort: rotate/revoke the refresh cookie by calling refresh with
      // a no-op intent. The backend treats a missing/expired cookie as 401.
      // We simply clear local state and let the cookie be overwritten on
      // next login. A dedicated revoke endpoint can be added later.
      setAccessToken(null);
      setUser({ email: undefined, tenantName: undefined, tenantId: undefined });
    } finally {
      loggingOut.current = false;
    }
  }, []);

  // Wire the HAL singleton to read the in-memory token and react to 401s.
  useEffect(() => {
    configureHalClient({
      getAuthToken: () => accessToken,
      onUnauthorized: () => {
        // Clear the token; the routing guard will redirect to /login.
        setAccessToken(null);
      },
    });
  }, [accessToken]);

  const value = useMemo<AuthContextValue>(
    () => ({
      accessToken,
      user,
      isAuthenticated: !!accessToken,
      bootstrapped,
      login,
      logout,
    }),
    [accessToken, user, bootstrapped, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within <AuthProvider>');
  return ctx;
}