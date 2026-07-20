import { describe, expect, it } from "vitest";
import { buildAuthorizeUrl, type OidcConfig, parseCallback, pkceChallenge, randomString } from "./oidc";

const config: OidcConfig = {
  authority: "https://idp.test",
  clientId: "apidiff-dashboard",
  scope: "openid email",
  redirectUri: "https://app.test/callback",
};

describe("pkceChallenge", () => {
  it("matches the RFC 7636 S256 test vector", async () => {
    // Verifier and expected challenge from RFC 7636 Appendix B.
    const challenge = await pkceChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk");
    expect(challenge).toBe("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM");
  });
});

describe("randomString", () => {
  it("is url-safe and unique", () => {
    const a = randomString();
    const b = randomString();
    expect(a).not.toBe(b);
    expect(a).toMatch(/^[A-Za-z0-9_-]+$/);
  });
});

describe("buildAuthorizeUrl", () => {
  it("includes PKCE and required params", () => {
    const url = new URL(buildAuthorizeUrl(config, "https://idp.test/authorize", "state123", "chal456"));
    expect(url.searchParams.get("response_type")).toBe("code");
    expect(url.searchParams.get("client_id")).toBe("apidiff-dashboard");
    expect(url.searchParams.get("redirect_uri")).toBe("https://app.test/callback");
    expect(url.searchParams.get("code_challenge")).toBe("chal456");
    expect(url.searchParams.get("code_challenge_method")).toBe("S256");
    expect(url.searchParams.get("state")).toBe("state123");
  });
});

describe("parseCallback", () => {
  it("extracts code, state, and error", () => {
    expect(parseCallback("?code=abc&state=xyz")).toEqual({ code: "abc", state: "xyz", error: null });
    expect(parseCallback("?error=access_denied")).toEqual({ code: null, state: null, error: "access_denied" });
  });
});
