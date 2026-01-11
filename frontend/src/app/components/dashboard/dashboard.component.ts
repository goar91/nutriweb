import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="dashboard-container">
      <div class="dashboard-header">
        <h1>Dashboard</h1>
        <p>Bienvenido a tu panel de control nutricional</p>
      </div>

      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon patients">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"/>
            </svg>
          </div>
          <div class="stat-content">
            <h3>Pacientes</h3>
            <p class="stat-number">125</p>
            <span class="stat-label">Pacientes activos</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon histories">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M19 3h-4.18C14.4 1.84 13.3 1 12 1c-1.3 0-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm2 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"/>
            </svg>
          </div>
          <div class="stat-content">
            <h3>Historias</h3>
            <p class="stat-number">342</p>
            <span class="stat-label">Historias registradas</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon consultations">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M11.99 2C6.47 2 2 6.48 2 12s4.47 10 9.99 10C17.52 22 22 17.52 22 12S17.52 2 11.99 2zM12 20c-4.42 0-8-3.58-8-8s3.58-8 8-8 8 3.58 8 8-3.58 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67z"/>
            </svg>
          </div>
          <div class="stat-content">
            <h3>Consultas</h3>
            <p class="stat-number">28</p>
            <span class="stat-label">Este mes</span>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon plans">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="currentColor">
              <path d="M9 11H7v2h2v-2zm4 0h-2v2h2v-2zm4 0h-2v2h2v-2zm2-7h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V9h14v11z"/>
            </svg>
          </div>
          <div class="stat-content">
            <h3>Planes</h3>
            <p class="stat-number">45</p>
            <span class="stat-label">Planes activos</span>
          </div>
        </div>
      </div>

      <div class="action-cards">
        <div class="action-card primary">
          <h3>Nueva Historia Clínica</h3>
          <p>Registra la información nutricional de un nuevo paciente</p>
          <a routerLink="/history" class="action-btn">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z" clip-rule="evenodd"/>
            </svg>
            Crear historia
          </a>
        </div>

        <div class="action-card secondary">
          <h3>Ver Pacientes</h3>
          <p>Consulta y gestiona la lista completa de pacientes</p>
          <button class="action-btn" disabled>
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z"/>
            </svg>
            Próximamente
          </button>
        </div>

        <div class="action-card secondary">
          <h3>Reportes</h3>
          <p>Genera reportes y estadísticas de tus pacientes</p>
          <button class="action-btn" disabled>
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z"/>
            </svg>
            Próximamente
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 2rem 1rem;
    }

    .dashboard-header {
      margin-bottom: 2.5rem;
    }

    .dashboard-header h1 {
      margin: 0 0 0.5rem;
      font-size: 2.5rem;
      color: #1f2937;
      font-weight: 700;
    }

    .dashboard-header p {
      margin: 0;
      color: #6b7280;
      font-size: 1.1rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
      margin-bottom: 3rem;
    }

    .stat-card {
      background: white;
      padding: 1.75rem;
      border-radius: 16px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      display: flex;
      gap: 1.25rem;
      align-items: flex-start;
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .stat-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
    }

    .stat-icon {
      width: 56px;
      height: 56px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
    }

    .stat-icon.patients {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .stat-icon.histories {
      background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
      color: white;
    }

    .stat-icon.consultations {
      background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
      color: white;
    }

    .stat-icon.plans {
      background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
      color: white;
    }

    .stat-content h3 {
      margin: 0 0 0.5rem;
      font-size: 0.9rem;
      color: #6b7280;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .stat-number {
      margin: 0 0 0.25rem;
      font-size: 2rem;
      font-weight: 700;
      color: #1f2937;
    }

    .stat-label {
      font-size: 0.875rem;
      color: #9ca3af;
    }

    .action-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1.5rem;
    }

    .action-card {
      background: white;
      padding: 2rem;
      border-radius: 16px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      transition: transform 0.2s, box-shadow 0.2s;
    }

    .action-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 12px 24px rgba(0, 0, 0, 0.15);
    }

    .action-card h3 {
      margin: 0 0 0.75rem;
      font-size: 1.5rem;
      color: #1f2937;
    }

    .action-card p {
      margin: 0 0 1.5rem;
      color: #6b7280;
      line-height: 1.6;
    }

    .action-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.875rem 1.5rem;
      border: none;
      border-radius: 12px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
      text-decoration: none;
    }

    .primary .action-btn {
      background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
      color: white;
      box-shadow: 0 4px 12px rgba(99, 102, 241, 0.4);
    }

    .primary .action-btn:hover {
      box-shadow: 0 6px 20px rgba(99, 102, 241, 0.5);
      transform: translateY(-2px);
    }

    .secondary .action-btn {
      background: #f3f4f6;
      color: #6b7280;
    }

    .secondary .action-btn:hover:not(:disabled) {
      background: #e5e7eb;
    }

    .action-btn:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }

    @media (max-width: 768px) {
      .dashboard-header h1 {
        font-size: 2rem;
      }

      .stats-grid {
        grid-template-columns: 1fr;
      }

      .action-cards {
        grid-template-columns: 1fr;
      }

      .stat-card {
        padding: 1.25rem;
      }

      .stat-icon {
        width: 48px;
        height: 48px;
      }
    }
  `]
})
export class DashboardComponent {
  constructor(public auth: AuthService) {}
}
