import { createContext, type ReactNode, useContext, useMemo, useState } from "react";
import { Navigate } from "react-router-dom";
import { clearToken, getToken, setToken } from "./lib/session";

interface Session {
  token: string | null;
  signIn: (token: string) => void;
  signOut: () => void;
}

const SessionContext = createContext<Session | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [token, setTok] = useState<string | null>(getToken());

  const value = useMemo<Session>(
    () => ({
      token,
      signIn: (t: string) => {
        setToken(t);
        setTok(t);
      },
      signOut: () => {
        clearToken();
        setTok(null);
      },
    }),
    [token],
  );

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession(): Session {
  const ctx = useContext(SessionContext);
  if (!ctx) {
    throw new Error("useSession must be used within a SessionProvider");
  }
  return ctx;
}

export function RequireAuth({ children }: { children: ReactNode }) {
  const { token } = useSession();
  if (!token) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}
