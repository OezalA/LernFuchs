import { Injectable, signal } from '@angular/core';
import {
  PublicClientApplication, AccountInfo, InteractionRequiredAuthError
} from '@azure/msal-browser';

// Diese IDs sind keine Geheimnisse (öffentliche Bezeichner der Entra-App-Registrierung).
const CLIENT_ID = 'f9186456-9d9c-48ca-928e-5be739a196cc';
const TENANT_ID = '0da38468-bedc-424c-9a81-584ca956d3af';
export const ADMIN_SCOPE = `api://${CLIENT_ID}/access_as_admin`;

/**
 * Anmeldung über Microsoft Entra ID (nur für den Admin-Bereich), per Redirect-Flow:
 * die Seite wechselt zu Microsoft und kommt nach /admin zurück, wo MSAL die Antwort verarbeitet.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private msal = new PublicClientApplication({
    auth: {
      clientId: CLIENT_ID,
      authority: `https://login.microsoftonline.com/${TENANT_ID}`,
      redirectUri: `${window.location.origin}/admin`,
      postLogoutRedirectUri: `${window.location.origin}/admin`,
    },
    cache: { cacheLocation: 'localStorage' },
  });

  private _account = signal<AccountInfo | null>(null);
  /** Aktuell angemeldeter Benutzer (oder null). */
  account = this._account.asReadonly();

  private ready: Promise<void>;

  constructor() {
    this.ready = this.msal.initialize()
      .then(() => this.msal.handleRedirectPromise()) // verarbeitet die Antwort nach dem Redirect
      .then(result => {
        const acc = result?.account ?? this.msal.getAllAccounts()[0] ?? null;
        if (acc) { this.msal.setActiveAccount(acc); this._account.set(acc); }
      })
      .catch(() => { /* keine ausstehende Anmeldung */ });
  }

  /** Hat der angemeldete Benutzer die App-Rolle "Admin"? */
  get isAdmin(): boolean {
    const roles = (this._account()?.idTokenClaims as { roles?: string[] } | undefined)?.roles;
    return !!roles?.includes('Admin');
  }

  get userName(): string {
    return this._account()?.name ?? this._account()?.username ?? '';
  }

  /** Startet die Anmeldung (Seiten-Redirect zu Microsoft). */
  async login(): Promise<void> {
    await this.ready;
    await this.msal.loginRedirect({ scopes: [ADMIN_SCOPE] });
  }

  async logout(): Promise<void> {
    await this.ready;
    await this.msal.logoutRedirect({ account: this._account() ?? undefined });
  }

  /** Access-Token für die geschützte API (leise; sonst per Redirect neu anmelden). */
  async getToken(): Promise<string | null> {
    await this.ready;
    const account = this._account() ?? this.msal.getAllAccounts()[0] ?? null;
    if (!account) return null;
    try {
      const res = await this.msal.acquireTokenSilent({ scopes: [ADMIN_SCOPE], account });
      return res.accessToken;
    } catch (e) {
      if (e instanceof InteractionRequiredAuthError) {
        await this.msal.acquireTokenRedirect({ scopes: [ADMIN_SCOPE], account });
        return null;
      }
      throw e;
    }
  }
}
