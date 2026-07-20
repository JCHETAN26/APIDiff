import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSession } from "../auth";
import { completeLogin, oidcConfig, parseCallback } from "../lib/oidc";

/** Handles the OIDC redirect: exchanges the code for a token, then goes home. */
export function CallbackPage() {
  const { signIn } = useSession();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const ran = useRef(false);

  useEffect(() => {
    if (ran.current) return; // guard React 18/19 StrictMode double-invoke
    ran.current = true;

    const config = oidcConfig();
    if (!config) {
      navigate("/login", { replace: true });
      return;
    }

    completeLogin(config, parseCallback(window.location.search))
      .then((token) => {
        signIn(token);
        navigate("/", { replace: true });
      })
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : "Sign-in failed");
      });
  }, [navigate, signIn]);

  return (
    <div className="card login">
      {error ? (
        <>
          <h1>Sign-in failed</h1>
          <p className="error">{error}</p>
          <button type="button" onClick={() => navigate("/login", { replace: true })}>
            Back to sign in
          </button>
        </>
      ) : (
        <p className="muted">Completing sign-in…</p>
      )}
    </div>
  );
}
