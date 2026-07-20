import type { ReactNode } from "react";
import { BrowserRouter, Link, Navigate, Route, Routes } from "react-router-dom";
import { RequireAuth, SessionProvider, useSession } from "./auth";
import { CallbackPage } from "./pages/CallbackPage";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { RunPage } from "./pages/RunPage";
import { RunsPage } from "./pages/RunsPage";

function Layout({ children }: { children: ReactNode }) {
  const { token, signOut } = useSession();
  return (
    <div className="app">
      <header className="topbar">
        <Link to="/" className="brand">
          APIDiff
        </Link>
        {token ? (
          <button type="button" className="ghost" onClick={signOut}>
            Sign out
          </button>
        ) : null}
      </header>
      <main className="content">{children}</main>
    </div>
  );
}

export default function App() {
  return (
    <SessionProvider>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/callback" element={<CallbackPage />} />
            <Route
              path="/"
              element={
                <RequireAuth>
                  <HomePage />
                </RequireAuth>
              }
            />
            <Route
              path="/orgs/:orgId/projects/:projectId/runs"
              element={
                <RequireAuth>
                  <RunsPage />
                </RequireAuth>
              }
            />
            <Route
              path="/orgs/:orgId/projects/:projectId/runs/:runId"
              element={
                <RequireAuth>
                  <RunPage />
                </RequireAuth>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </SessionProvider>
  );
}
