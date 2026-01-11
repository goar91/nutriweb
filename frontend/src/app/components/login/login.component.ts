import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <div class="logo">
            <svg width="48" height="48" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect width="48" height="48" rx="12" fill="#6366f1"/>
              <path d="M24 12C17.373 12 12 17.373 12 24C12 30.627 17.373 36 24 36C30.627 36 36 30.627 36 24C36 17.373 30.627 12 24 12ZM24 33C19.029 33 15 28.971 15 24C15 19.029 19.029 15 24 15C28.971 15 33 19.029 33 24C33 28.971 28.971 33 24 33Z" fill="white"/>
              <path d="M24 18C20.686 18 18 20.686 18 24C18 27.314 20.686 30 24 30C27.314 30 30 27.314 30 24C30 20.686 27.314 18 24 18ZM24 27C22.343 27 21 25.657 21 24C21 22.343 22.343 21 24 21C25.657 21 27 22.343 27 24C27 25.657 25.657 27 24 27Z" fill="white"/>
            </svg>
          </div>
          <h1>NutriWeb</h1>
          <p class="subtitle">Plataforma de gestión nutricional</p>
        </div>

        <div class="login-body">
          <h2>Bienvenido</h2>
          <p class="description">
            Inicia sesión para acceder a la plataforma de historias clínicas nutricionales
          </p>

          <button 
            class="login-btn" 
            (click)="loginWithRedirect()"
            *ngIf="!(auth.isAuthenticated$ | async)">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M10 0C4.486 0 0 4.486 0 10C0 15.514 4.486 20 10 20C15.514 20 20 15.514 20 10C20 4.486 15.514 0 10 0ZM10 3C11.657 3 13 4.343 13 6C13 7.657 11.657 9 10 9C8.343 9 7 7.657 7 6C7 4.343 8.343 3 10 3ZM10 17C7.33 17 5.02 15.45 3.88 13.16C3.91 10.82 8.56 9.53 10 9.53C11.43 9.53 16.09 10.82 16.12 13.16C14.98 15.45 12.67 17 10 17Z" fill="white"/>
            </svg>
            Iniciar sesión con Auth0
          </button>

          <div class="user-info" *ngIf="auth.isAuthenticated$ | async">
            <div class="avatar" *ngIf="(auth.user$ | async)?.picture">
              <img [src]="(auth.user$ | async)?.picture" [alt]="(auth.user$ | async)?.name" />
            </div>
            <div class="user-details">
              <p class="user-name">{{ (auth.user$ | async)?.name }}</p>
              <p class="user-email">{{ (auth.user$ | async)?.email }}</p>
            </div>
            <button class="logout-btn" (click)="logout()">
              Cerrar sesión
            </button>
          </div>
        </div>

        <div class="login-footer">
          <p>¿Necesitas ayuda? <a href="#">Contáctanos</a></p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 1rem;
    }

    .login-card {
      background: white;
      border-radius: 24px;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      max-width: 440px;
      width: 100%;
      overflow: hidden;
      animation: slideUp 0.6s ease-out;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(30px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .login-header {
      background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
      padding: 3rem 2rem 2rem;
      text-align: center;
      color: white;
    }

    .logo {
      margin: 0 auto 1rem;
      width: fit-content;
    }

    .login-header h1 {
      margin: 0 0 0.5rem;
      font-size: 2rem;
      font-weight: 700;
    }

    .subtitle {
      margin: 0;
      opacity: 0.95;
      font-size: 1rem;
    }

    .login-body {
      padding: 2.5rem 2rem;
    }

    .login-body h2 {
      margin: 0 0 0.5rem;
      font-size: 1.75rem;
      color: #1f2937;
    }

    .description {
      margin: 0 0 2rem;
      color: #6b7280;
      line-height: 1.6;
    }

    .login-btn {
      width: 100%;
      padding: 1rem 1.5rem;
      background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
      color: white;
      border: none;
      border-radius: 12px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      transition: all 0.3s ease;
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
    }

    .login-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(99, 102, 241, 0.5);
    }

    .login-btn:active {
      transform: translateY(0);
    }

    .user-info {
      background: #f9fafb;
      border-radius: 12px;
      padding: 1.5rem;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .avatar {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      overflow: hidden;
      border: 4px solid white;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .avatar img {
      width: 100%;
      height: 100%;
      object-fit: cover;
    }

    .user-details {
      text-align: center;
    }

    .user-name {
      margin: 0 0 0.25rem;
      font-size: 1.25rem;
      font-weight: 600;
      color: #1f2937;
    }

    .user-email {
      margin: 0;
      color: #6b7280;
      font-size: 0.9rem;
    }

    .logout-btn {
      width: 100%;
      padding: 0.75rem 1.5rem;
      background: white;
      color: #6366f1;
      border: 2px solid #6366f1;
      border-radius: 12px;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s ease;
    }

    .logout-btn:hover {
      background: #6366f1;
      color: white;
    }

    .login-footer {
      padding: 1.5rem 2rem;
      background: #f9fafb;
      text-align: center;
      border-top: 1px solid #e5e7eb;
    }

    .login-footer p {
      margin: 0;
      color: #6b7280;
      font-size: 0.9rem;
    }

    .login-footer a {
      color: #6366f1;
      text-decoration: none;
      font-weight: 600;
    }

    .login-footer a:hover {
      text-decoration: underline;
    }

    @media (max-width: 480px) {
      .login-card {
        border-radius: 16px;
      }

      .login-header {
        padding: 2rem 1.5rem 1.5rem;
      }

      .login-header h1 {
        font-size: 1.5rem;
      }

      .login-body {
        padding: 2rem 1.5rem;
      }

      .login-body h2 {
        font-size: 1.5rem;
      }
    }
  `]
})
export class LoginComponent {
  constructor(public auth: AuthService) {}

  loginWithRedirect(): void {
    this.auth.loginWithRedirect();
  }

  logout(): void {
    this.auth.logout({ logoutParams: { returnTo: window.location.origin } });
  }
}
