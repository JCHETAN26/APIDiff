import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSession } from "../auth";
import { beginLogin, oidcConfig } from "../lib/oidc";

export function LoginPage() {
  const { signIn } = useSession();
  const navigate = useNavigate();
  const [value, setValue] = useState("");
  const [error, setError] = useState<string | null>(null);
  const config = oidcConfig();

  async function sso() {
    setError(null);
    try {
      await beginLogin(config!);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sign-in failed");
    }
  }

  function submitToken(event: FormEvent) {
    event.preventDefault();
    const token = value.trim();
    if (token) {
      signIn(token);
      navigate("/");
    }
  }

  if (config) {
    return (
      <div className="card login">
        <h1>Sign in</h1>
        <p className="muted">Continue with your organization's single sign-on.</p>
        <button type="button" onClick={sso}>
          Sign in with SSO
        </button>
        {error ? <p className="error">{error}</p> : null}
      </div>
    );
  }

  return (
    <form className="card login" onSubmit={submitToken}>
      <h1>Sign in</h1>
      <p className="muted">
        Paste an API bearer token to review runs. Production deployments use OIDC single sign-on
        (set <code>VITE_OIDC_AUTHORITY</code> and <code>VITE_OIDC_CLIENT_ID</code>).
      </p>
      <textarea
        value={value}
        onChange={(event) => setValue(event.target.value)}
        placeholder="eyJhbGciOi…"
        rows={4}
        aria-label="Bearer token"
      />
      <button type="submit" disabled={value.trim() === ""}>
        Continue
      </button>
    </form>
  );
}
