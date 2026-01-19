import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';

export interface User {
  id: string;
  username: string;
  nombre: string;
  email: string;
}

export interface LoginResponse {
  success: boolean;
  token: string;
  user: User;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'http://localhost:5000/api/auth';
  private tokenKey = 'auth_token';
  private userKey = 'auth_user';
  
  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();
  
  public isAuthenticated = signal<boolean>(this.hasToken());

  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, { username, password })
      .pipe(
        tap({
          next: (response) => {
            if (response.success && response.token) {
              this.setSession(response.token, response.user);
            }
          },
          error: () => {
            this.clearSession();
          }
        })
      );
  }

  register(username: string, password: string, email?: string, nombre?: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/register`, { username, password, email, nombre })
      .pipe(
        tap({
          next: (response) => {
            if (response.success && response.token) {
              this.setSession(response.token, response.user);
            }
          }
        })
      );
  }

  logout(): void {
    const token = this.getToken();
    const shutdown = this.shouldShutdownOnLogout();
    const url = shutdown ? `${this.apiUrl}/logout?shutdown=1` : `${this.apiUrl}/logout`;

    if (shutdown) {
      this.clearSession();
      this.sendLogoutRequest(url);
      this.tryCloseWindow();
      return;
    }

    if (token) {
      this.http.post(url, {}).subscribe({
        error: () => {
          console.error('Error en logout');
        }
      });
    }

    this.clearSession();
    this.router.navigate(['/login']);
  }

  verifyToken(): Observable<any> {
    return this.http.get(`${this.apiUrl}/verify`).pipe(
      tap({
        next: (response: any) => {
          if (response.valid && response.user) {
            this.currentUserSubject.next(response.user);
            this.isAuthenticated.set(true);
          }
        },
        error: () => {
          this.clearSession();
        }
      })
    );
  }

  private setSession(token: string, user: User): void {
    localStorage.setItem(this.tokenKey, token);
    localStorage.setItem(this.userKey, JSON.stringify(user));
    this.currentUserSubject.next(user);
    this.isAuthenticated.set(true);
  }

  private clearSession(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.currentUserSubject.next(null);
    this.isAuthenticated.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  private getUserFromStorage(): User | null {
    const userStr = localStorage.getItem(this.userKey);
    return userStr ? JSON.parse(userStr) : null;
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.tokenKey);
  }

  private shouldShutdownOnLogout(): boolean {
    if (this.isLocalHost(window.location.hostname)) {
      return true;
    }

    try {
      const apiHost = new URL(this.apiUrl).hostname;
      return this.isLocalHost(apiHost);
    } catch {
      return false;
    }
  }

  private isLocalHost(hostname: string): boolean {
    const normalized = hostname.toLowerCase();
    return normalized === 'localhost' || normalized === '127.0.0.1' || normalized === '::1';
  }

  private sendLogoutRequest(url: string): void {
    try {
      if (navigator.sendBeacon) {
        const queued = navigator.sendBeacon(url);
        if (queued) {
          return;
        }
      }
    } catch {
      // Fall back to fetch.
    }

    fetch(url, {
      method: 'POST',
      keepalive: true,
      mode: 'no-cors'
    }).catch(() => {
      // Best-effort shutdown; ignore errors while closing.
    });
  }

  private tryCloseWindow(): void {
    window.open('', '_self');
    window.close();

    setTimeout(() => {
      if (!document.hidden) {
        window.location.replace('about:blank');
      }
    }, 250);
  }
}
