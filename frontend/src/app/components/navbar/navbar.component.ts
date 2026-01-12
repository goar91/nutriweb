import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <nav class="navbar">
      <div class="nav-container">
        <div class="nav-brand">
          <svg width="32" height="32" viewBox="0 0 48 48" fill="none" xmlns="http://www.w3.org/2000/svg">
            <rect width="48" height="48" rx="12" fill="#6366f1"/>
            <path d="M24 12C17.373 12 12 17.373 12 24C12 30.627 17.373 36 24 36C30.627 36 36 30.627 36 24C36 17.373 30.627 12 24 12ZM24 33C19.029 33 15 28.971 15 24C15 19.029 19.029 15 24 15C28.971 15 33 19.029 33 24C33 28.971 28.971 33 24 33Z" fill="white"/>
            <path d="M24 18C20.686 18 18 20.686 18 24C18 27.314 20.686 30 24 30C27.314 30 30 27.314 30 24C30 20.686 27.314 18 24 18ZM24 27C22.343 27 21 25.657 21 24C21 22.343 22.343 21 24 21C25.657 21 27 22.343 27 24C27 25.657 25.657 27 24 27Z" fill="white"/>
          </svg>
          <span class="brand-name">NutriWeb</span>
        </div>

        <button class="mobile-toggle" (click)="toggleMobileMenu()" aria-label="Toggle menu">
          <svg *ngIf="!mobileMenuOpen" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/>
          </svg>
          <svg *ngIf="mobileMenuOpen" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>

        <div class="nav-menu" [class.active]="mobileMenuOpen">
          <a routerLink="/dashboard" class="nav-link" (click)="closeMobileMenu()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z"/>
            </svg>
            Dashboard
          </a>
          
          <a routerLink="/history" class="nav-link" (click)="closeMobileMenu()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z"/>
              <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z" clip-rule="evenodd"/>
            </svg>
            Nueva Historia
          </a>

          <a routerLink="/pacientes" class="nav-link" (click)="closeMobileMenu()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z"/>
            </svg>
            Ver Pacientes
          </a>

          <a routerLink="/reportes" class="nav-link" (click)="closeMobileMenu()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z"/>
            </svg>
            Reportes
          </a>

          <div class="nav-user" *ngIf="authService.currentUser$ | async as user">
            <div class="user-avatar">
              {{ user.nombre ? user.nombre.charAt(0).toUpperCase() : user.username.charAt(0).toUpperCase() }}
            </div>
            <div class="user-info">
              <span class="user-name">{{ user.nombre || user.username }}</span>
              <button class="logout-link" (click)="logout()">Cerrar sesión</button>
            </div>
          </div>
        </div>
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      background: white;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      position: sticky;
      top: 0;
      z-index: 1000;
    }

    .nav-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0.75rem 1rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 2rem;
    }

    .nav-brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-weight: 700;
      font-size: 1.25rem;
      color: #1f2937;
    }

    .brand-name {
      display: none;
    }

    .mobile-toggle {
      display: none;
      background: none;
      border: none;
      color: #4b5563;
      cursor: pointer;
      padding: 0.5rem;
    }

    .nav-menu {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      margin-left: auto;
    }

    .nav-link {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      color: #4b5563;
      text-decoration: none;
      border-radius: 8px;
      font-weight: 500;
      transition: all 0.2s;
    }

    .nav-link:hover {
      background: #f3f4f6;
      color: #6366f1;
    }

    .nav-user {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.5rem 1rem;
      background: #f9fafb;
      border-radius: 999px;
    }

    .user-avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 0.875rem;
    }

    .user-info {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .user-name {
      font-size: 0.875rem;
      font-weight: 600;
      color: #1f2937;
    }

    .logout-link {
      background: none;
      border: none;
      color: #6366f1;
      font-size: 0.75rem;
      cursor: pointer;
      padding: 0;
      text-align: left;
      font-weight: 500;
    }

    .logout-link:hover {
      text-decoration: underline;
    }

    @media (min-width: 640px) {
      .brand-name {
        display: inline;
      }
    }

    @media (max-width: 768px) {
      .mobile-toggle {
        display: block;
      }

      .nav-menu {
        position: fixed;
        top: 60px;
        left: 0;
        right: 0;
        background: white;
        flex-direction: column;
        align-items: stretch;
        padding: 1rem;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        transform: translateY(-120%);
        transition: transform 0.3s ease;
      }

      .nav-menu.active {
        transform: translateY(0);
      }

      .nav-link {
        padding: 1rem;
        justify-content: flex-start;
      }

      .nav-user {
        border-radius: 12px;
        padding: 1rem;
      }

      .user-info {
        gap: 0.25rem;
      }
    }
  `]
})
export class NavbarComponent {
  mobileMenuOpen = false;

  constructor(public authService: AuthService) {}

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  closeMobileMenu(): void {
    this.mobileMenuOpen = false;
  }

  logout(): void {
    this.authService.logout();
  }
}
