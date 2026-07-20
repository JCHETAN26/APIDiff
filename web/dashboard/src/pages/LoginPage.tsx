import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useSession } from "../auth";

export function LoginPage() {
  const { signIn } = useSession();
  const navigate = useNavigate();
  const [value, setValue] = useState("");

  function submit(event: FormEvent) {
    event.preventDefault();
    const token = value.trim();
    if (token) {
      signIn(token);
      navigate("/");
    }
  }

  return (
    <form className="card login" onSubmit={submit}>
      <h1>Sign in</h1>
      <p className="muted">
        Paste an API bearer token to review runs. Production deployments use OIDC single sign-on.
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
