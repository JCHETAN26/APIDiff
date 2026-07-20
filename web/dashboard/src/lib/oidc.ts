/** OpenID Connect Authorization Code + PKCE flow for the dashboard SPA. */

export interface OidcConfig {
  authority: string;
  clientId: string;
  scope: string;
  redirectUri: string;
}

const VERIFIER_KEY = "apidiff.oidc.verifier";
const STATE_KEY = "apidiff.oidc.state";

/** Reads OIDC config from Vite env; null when not configured (dev/pasted-token). */
export function oidcConfig(): OidcConfig | null {
  const authority = import.meta.env.VITE_OIDC_AUTHORITY as string | undefined;
  const clientId = import.meta.env.VITE_OIDC_CLIENT_ID as string | undefined;
  if (!authority || !clientId) {
    return null;
  }
  return {
    authority: authority.replace(/\/$/, ""),
    clientId,
    scope: (import.meta.env.VITE_OIDC_SCOPE as string | undefined) ?? "openid profile email",
    redirectUri: `${window.location.origin}/callback`,
  };
}

function base64UrlEncode(bytes: Uint8Array): string {
  let binary = "";
  for (const b of bytes) binary += String.fromCharCode(b);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

/** A URL-safe random string (used for the PKCE verifier and state). */
export function randomString(bytes = 32): string {
  const buf = new Uint8Array(bytes);
  crypto.getRandomValues(buf);
  return base64UrlEncode(buf);
}

/** S256 PKCE challenge = base64url(sha256(verifier)). */
export async function pkceChallenge(verifier: string): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
  return base64UrlEncode(new Uint8Array(digest));
}

/** Builds the authorization endpoint URL with all required query params. */
export function buildAuthorizeUrl(
  config: OidcConfig,
  authorizationEndpoint: string,
  state: string,
  codeChallenge: string,
): string {
  const url = new URL(authorizationEndpoint);
  const params: Record<string, string> = {
    response_type: "code",
    client_id: config.clientId,
    redirect_uri: config.redirectUri,
    scope: config.scope,
    state,
    code_challenge: codeChallenge,
    code_challenge_method: "S256",
  };
  for (const [key, value] of Object.entries(params)) {
    url.searchParams.set(key, value);
  }
  return url.toString();
}

export interface CallbackParams {
  code: string | null;
  state: string | null;
  error: string | null;
}

/** Parses the code / state / error from a callback query string. */
export function parseCallback(search: string): CallbackParams {
  const params = new URLSearchParams(search);
  return {
    code: params.get("code"),
    state: params.get("state"),
    error: params.get("error"),
  };
}

interface Discovery {
  authorization_endpoint: string;
  token_endpoint: string;
}

async function discover(authority: string): Promise<Discovery> {
  const response = await fetch(`${authority}/.well-known/openid-configuration`);
  if (!response.ok) {
    throw new Error("OIDC discovery failed");
  }
  return (await response.json()) as Discovery;
}

/** Starts the login: stashes verifier+state and redirects to the IdP. */
export async function beginLogin(config: OidcConfig): Promise<void> {
  const verifier = randomString();
  const state = randomString(16);
  sessionStorage.setItem(VERIFIER_KEY, verifier);
  sessionStorage.setItem(STATE_KEY, state);

  const { authorization_endpoint } = await discover(config.authority);
  const challenge = await pkceChallenge(verifier);
  window.location.assign(buildAuthorizeUrl(config, authorization_endpoint, state, challenge));
}

/** Completes the callback: validates state, exchanges the code, returns the token. */
export async function completeLogin(config: OidcConfig, params: CallbackParams): Promise<string> {
  if (params.error) {
    throw new Error(`Sign-in failed: ${params.error}`);
  }
  const verifier = sessionStorage.getItem(VERIFIER_KEY);
  const expectedState = sessionStorage.getItem(STATE_KEY);
  sessionStorage.removeItem(VERIFIER_KEY);
  sessionStorage.removeItem(STATE_KEY);

  if (!params.code || !params.state || params.state !== expectedState || !verifier) {
    throw new Error("Invalid sign-in callback");
  }

  const { token_endpoint } = await discover(config.authority);
  const response = await fetch(token_endpoint, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "authorization_code",
      code: params.code,
      redirect_uri: config.redirectUri,
      client_id: config.clientId,
      code_verifier: verifier,
    }),
  });
  if (!response.ok) {
    throw new Error("Token exchange failed");
  }

  const tokens = (await response.json()) as { access_token?: string; id_token?: string };
  const token = tokens.access_token ?? tokens.id_token;
  if (!token) {
    throw new Error("Token response had no access_token");
  }
  return token;
}
