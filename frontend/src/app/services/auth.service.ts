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
          error: (error) => {
            console.error('Error en login:', error);
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
          },
          error: (error) => {
            console.error('Error en registro:', error);
          }
        })
      );
  }

  logout(): void {
    const token = this.getToken();
    if (token) {
      this.http.post(`${this.apiUrl}/logout`, {}).subscribe({
        error: (error) => console.error('Error en logout:', error)
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
}
