import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { NutritionService } from '../../nutrition.service';
import { EditDataService } from '../../services/edit-data.service';
import { PlanesAlimentacionComponent } from '../planes-alimentacion/planes-alimentacion.component';

interface Paciente {
  id: string;
  nombre: string;
  numero_cedula: string;
  email: string;
  telefono: string;
  edad_cronologica: string;
  historias_clinicas?: HistoriaClinica[];
}

interface HistoriaClinica {
  id: string;
  fecha_consulta: string;
  motivo_consulta: string;
  diagnostico: string;
  notas_extras: string;
  fecha_registro: Date;
}

interface DatosAntropometricos {
  peso: string;
  talla: string;
  imc: string;
  circunferencia_cintura: string;
  circunferencia_cadera: string;
  circunferencia_brazo: string;
  masa_muscular: string;
  grasa_corporal_porcentaje: string;
  grasa_corporal: string;
  grasa_visceral_porcentaje: string;
  edad: string;
  sexo: string;
  edad_metabolica: string;
  kcal_basales: string;
  actividad_fisica: string;
  circunferencia_pantorrilla: string;
  circunferencia_muslo: string;
  peso_ajustado: string;
  factor_actividad_fisica: string;
  tiempos_comida: string;
}

interface SignosVitales {
  presion_arterial: string;
  frecuencia_cardiaca: string;
  frecuencia_respiratoria: string;
  temperatura: string;
}

interface Antecedentes {
  apf: string;
  app: string;
  apq: string;
  ago: string;
  menarquia: string;
  p: string;
  g: string;
  c: string;
  a: string;
  alergias: string;
}

interface Habitos {
  fuma: string;
  alcohol: string;
  cafe: string;
  hidratacion: string;
  gaseosas: string;
  actividad_fisica: string;
  te: string;
  edulcorantes: string;
  alimentacion: string;
}

interface ValoresBioquimicos {
  glicemia: string;
  colesterol_total: string;
  trigliceridos: string;
  hdl: string;
  ldl: string;
  tgo: string;
  tgp: string;
  urea: string;
  creatinina: string;
}

interface Recordatorio24h {
  desayuno: string;
  snack1: string;
  almuerzo: string;
  snack2: string;
  cena: string;
  extras: string;
}

interface FrecuenciaConsumo {
  categoria: string;
  alimento: string;
  frecuencia: string;
}

interface PacienteDetallado {
  // Datos del paciente
  paciente_id: string;
  numero_cedula: string;
  nombre: string;
  edad_cronologica: string;
  sexo: string;
  email: string;
  telefono: string;
  lugar_residencia: string;
  estado_civil: string;
  ocupacion: string;
  fecha_creacion: Date;
  // Datos de la historia clínica específica
  historia_id: string;
  fecha_consulta: string;
  motivo_consulta: string;
  diagnostico: string;
  notas_extras: string;
  fecha_registro: Date;
  // Datos relacionados con esta historia
  datos_antropometricos: DatosAntropometricos | null;
  signos_vitales: SignosVitales | null;
  antecedentes: Antecedentes | null;
  habitos: Habitos | null;
  valores_bioquimicos: ValoresBioquimicos | null;
  recordatorio_24h: Recordatorio24h | null;
  frecuencia_consumo: FrecuenciaConsumo[];
}

interface ReporteData {
  totalPacientes: number;
  totalHistorias: number;
  pacientesPorGenero: { masculino: number; femenino: number };
  historiasUltimoMes: number;
  totalPlanes: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, PlanesAlimentacionComponent],
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
            <p class="stat-number">{{ reporteData().totalPacientes }}</p>
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
            <p class="stat-number">{{ reporteData().totalHistorias }}</p>
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
            <p class="stat-number">{{ reporteData().historiasUltimoMes }}</p>
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
            <p class="stat-number">{{ reporteData().totalPlanes }}</p>
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
          <button class="action-btn" (click)="togglePacientes()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M9 6a3 3 0 11-6 0 3 3 0 016 0zM17 6a3 3 0 11-6 0 3 3 0 016 0zM12.93 17c.046-.327.07-.66.07-1a6.97 6.97 0 00-1.5-4.33A5 5 0 0119 16v1h-6.07zM6 11a5 5 0 015 5v1H1v-1a5 5 0 015-5z"/>
            </svg>
            {{ showPacientes() ? 'Ocultar' : 'Ver Pacientes' }}
          </button>
        </div>

        <div class="action-card secondary">
          <h3>Reportes</h3>
          <p>Genera reportes y estadísticas de tus pacientes</p>
          <button class="action-btn" (click)="toggleReportes()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M2 11a1 1 0 011-1h2a1 1 0 011 1v5a1 1 0 01-1 1H3a1 1 0 01-1-1v-5zM8 7a1 1 0 011-1h2a1 1 0 011 1v9a1 1 0 01-1 1H9a1 1 0 01-1-1V7zM14 4a1 1 0 011-1h2a1 1 0 011 1v12a1 1 0 01-1 1h-2a1 1 0 01-1-1V4z"/>
            </svg>
            {{ showReportes() ? 'Ocultar' : 'Ver Reportes' }}
          </button>
        </div>

        <div class="action-card secondary">
          <h3>Planes</h3>
          <p>Crea y gestiona planes de alimentación semanales</p>
          <button class="action-btn" (click)="togglePlanes()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z"/>
              <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm9.707 5.707a1 1 0 00-1.414-1.414L9 12.586l-1.293-1.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
            </svg>
            {{ showPlanes() ? 'Ocultar' : 'Ver Planes' }}
          </button>
        </div>
      </div>

      <!-- Sección de Pacientes -->
      @if (showPacientes()) {
        <div class="section-container">
          <div class="section-header">
            <h2>Lista de Pacientes</h2>
            <div style="display: flex; justify-content: space-between; align-items: center;">
              <p>{{ pacientes().length }} pacientes registrados</p>
              <button class="action-btn detailed-report-btn" (click)="toggleReporteDetallado()">
                <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z"/>
                  <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z"/>
                </svg>
                {{ showReporteDetallado() ? 'Ocultar Reporte Detallado' : 'Ver Reporte Detallado' }}
              </button>
            </div>
          </div>

          @if (loadingPacientes()) {
            <div class="loading">
              <div class="spinner"></div>
              <p>Cargando pacientes...</p>
            </div>
          } @else if (pacientes().length === 0) {
            <div class="empty-state">
              <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z"/>
              </svg>
              <p>No hay pacientes registrados</p>
            </div>
          } @else {
            <div class="table-container">
              <table class="patients-table">
                <thead>
                  <tr>
                    <th>Nombre</th>
                    <th>Cédula</th>
                    <th>Edad</th>
                    <th>Email</th>
                    <th>Teléfono</th>
                    <th>Historias Clínicas</th>
                  </tr>
                </thead>
                <tbody>
                  @for (paciente of pacientes(); track paciente.id) {
                    <tr>
                      <td>{{ paciente.nombre || 'Sin nombre' }}</td>
                      <td>{{ paciente.numero_cedula || 'N/A' }}</td>
                      <td>{{ paciente.edad_cronologica || 'N/A' }}</td>
                      <td>{{ paciente.email || 'Sin email' }}</td>
                      <td>{{ paciente.telefono || 'Sin teléfono' }}</td>
                      <td>
                        @if (paciente.historias_clinicas && paciente.historias_clinicas.length > 0) {
                          <div class="historias-actions">
                            <span class="historias-count">{{ paciente.historias_clinicas.length }} historia(s)</span>
                            <div class="historias-buttons">
                              @for (historia of paciente.historias_clinicas; track historia.id; let i = $index) {
                                <button type="button" class="mini-edit-btn" (click)="editarHistoria(historia.id)" [title]="'Editar historia del ' + (historia.fecha_consulta || ('#' + (i + 1)))">
                                  <svg width="14" height="14" viewBox="0 0 20 20" fill="currentColor">
                                    <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z"/>
                                  </svg>
                                  HC {{ i + 1 }}
                                </button>
                              }
                            </div>
                          </div>
                        } @else {
                          <span class="no-historias">Sin historias</span>
                        }
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }

      <!-- Reporte Detallado de Pacientes -->
      @if (showReporteDetallado()) {
        <div class="section-container detailed-report">
          <div class="section-header">
            <h2>Reporte Detallado de Pacientes por Historia Clínica</h2>
            <p>Un reporte por cada fecha de consulta de cada paciente</p>
          </div>

          <!-- Buscador -->
          <div class="search-container">
            <div class="search-box">
              <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
              </svg>
              <input 
                type="text" 
                [(ngModel)]="searchTerm"
                (input)="onSearchChange()"
                placeholder="Buscar por nombre, cédula o fecha..."
                class="search-input"
              />
              @if (searchTerm()) {
                <button class="clear-search" (click)="clearSearch()">
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"/>
                  </svg>
                </button>
              }
            </div>
            <p class="search-results">{{ pacientesFiltrados().length }} resultados encontrados</p>
          </div>

          @if (loadingDetallado()) {
            <div class="loading">
              <div class="spinner"></div>
              <p>Cargando datos detallados...</p>
            </div>
          } @else if (pacientesFiltrados().length === 0) {
            <div class="empty-state">
              <svg width="64" height="64" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"/>
              </svg>
              <p>No se encontraron registros con ese criterio de búsqueda</p>
            </div>
          } @else {
            <div class="detailed-patients-list">
              @for (reporte of pacientesFiltrados(); track reporte.historia_id) {
                <div class="detailed-patient-card" [attr.data-historia-id]="reporte.historia_id">
                  <div class="patient-header">
                    <div class="patient-info">
                      <h3>{{ reporte.nombre }}</h3>
                      <div class="patient-meta">
                        <span class="badge">Fecha: {{ reporte.fecha_consulta || 'Sin fecha' }}</span>
                        <span class="badge">Cédula: {{ reporte.numero_cedula || 'N/A' }}</span>
                        <span class="badge">Edad: {{ reporte.edad_cronologica || 'N/A' }}</span>
                        <span class="badge">Sexo: {{ getSexoAbreviado(reporte.sexo) }}</span>
                      </div>
                    </div>
                    <div class="patient-actions">
                      <button class="edit-patient-data-btn" (click)="editarDatosPaciente(reporte.paciente_id)" title="Editar todos los datos del paciente">
                        <svg width="18" height="18" viewBox="0 0 20 20" fill="currentColor">
                          <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z"/>
                        </svg>
                        Editar
                      </button>
                      <button class="delete-patient-btn" (click)="eliminarPaciente(reporte.paciente_id, reporte.nombre)" title="Eliminar paciente">
                        <svg width="18" height="18" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/>
                        </svg>
                        Eliminar
                      </button>
                      <button class="expand-btn" (click)="togglePacienteExpanded(reporte.historia_id)">
                        <svg width="24" height="24" viewBox="0 0 20 20" fill="currentColor" 
                             [style.transform]="isPacienteExpanded(reporte.historia_id) ? 'rotate(180deg)' : 'rotate(0deg)'">
                          <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z"/>
                        </svg>
                      </button>
                    </div>
                  </div>

                  @if (isPacienteExpanded(reporte.historia_id)) {
                    <div class="patient-details">
                      <!-- Datos Básicos -->
                      <div class="detail-section">
                        <h4>Datos Personales</h4>
                        <div class="detail-grid">
                          <div class="detail-item">
                            <span class="label">Email:</span>
                            <span class="value">{{ reporte.email || 'No especificado' }}</span>
                          </div>
                          <div class="detail-item">
                            <span class="label">Teléfono:</span>
                            <span class="value">{{ reporte.telefono || 'No especificado' }}</span>
                          </div>
                          <div class="detail-item">
                            <span class="label">Residencia:</span>
                            <span class="value">{{ reporte.lugar_residencia || 'No especificado' }}</span>
                          </div>
                          <div class="detail-item">
                            <span class="label">Estado Civil:</span>
                            <span class="value">{{ reporte.estado_civil || 'No especificado' }}</span>
                          </div>
                          <div class="detail-item">
                            <span class="label">Ocupación:</span>
                            <span class="value">{{ reporte.ocupacion || 'No especificado' }}</span>
                          </div>
                        </div>
                      </div>

                      <!-- Historia Clínica -->
                      <div class="detail-section">
                        <h4>Historia Clínica</h4>
                        <div class="historia-card">
                          <div class="historia-header">
                            <span class="fecha">{{ reporte.fecha_consulta || 'Sin fecha' }}</span>
                            <div class="historia-buttons">
                              <button type="button" class="edit-historia-btn" (click)="editarHistoria(reporte.historia_id)" title="Editar historia clínica">
                                <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                                  <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z"/>
                                </svg>
                                Editar
                              </button>
                              <button type="button" class="print-historia-btn" (click)="imprimirHistoria(reporte.historia_id)" title="Imprimir esta historia clínica">
                                <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                                  <path fill-rule="evenodd" d="M5 4v3H4a2 2 0 00-2 2v3a2 2 0 002 2h1v2a2 2 0 002 2h6a2 2 0 002-2v-2h1a2 2 0 002-2V9a2 2 0 00-2-2h-1V4a2 2 0 00-2-2H7a2 2 0 00-2 2zm8 0H7v3h6V4zm0 8H7v4h6v-4z" clip-rule="evenodd"/>
                                </svg>
                                Imprimir
                              </button>
                              <button type="button" class="delete-historia-btn" (click)="eliminarHistoria(reporte.historia_id, reporte.nombre)" title="Eliminar esta historia clínica">
                                <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                                  <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/>
                                </svg>
                                Eliminar
                              </button>
                            </div>
                          </div>
                          <div class="historia-body">
                            <div class="historia-item">
                              <strong>Motivo:</strong> {{ reporte.motivo_consulta || 'No especificado' }}
                            </div>
                            <div class="historia-item">
                              <strong>Diagnóstico:</strong> {{ reporte.diagnostico || 'No especificado' }}
                            </div>
                            <div class="historia-item">
                              <strong>Notas Extra:</strong> {{ reporte.notas_extras || 'Sin notas adicionales' }}
                            </div>
                          </div>
                        </div>

                      </div>

                      <!-- Signos Vitales -->
                      <div class="detail-section">
                        <h4>Signos Vitales</h4>
                        @if (reporte.signos_vitales) {
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">Presión Arterial:</span>
                              <span class="value">{{ reporte.signos_vitales.presion_arterial || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Frecuencia Cardíaca:</span>
                              <span class="value">{{ reporte.signos_vitales.frecuencia_cardiaca || 'N/A' }} bpm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Frecuencia Respiratoria:</span>
                              <span class="value">{{ reporte.signos_vitales.frecuencia_respiratoria || 'N/A' }} rpm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Temperatura:</span>
                              <span class="value">{{ reporte.signos_vitales.temperatura || 'N/A' }} °C</span>
                            </div>
                          </div>
                        } @else {
                          <div class="no-data-message">
                            <p>No se registraron signos vitales en esta historia clínica</p>
                          </div>
                        }
                      </div>

                      <!-- Datos Antropométricos -->
                      <div class="detail-section">
                        <h4>Datos Antropométricos</h4>
                        @if (reporte.datos_antropometricos) {
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">Edad:</span>
                              <span class="value">{{ reporte.datos_antropometricos.edad || 'N/A' }} años</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Edad Metabólica:</span>
                              <span class="value">{{ reporte.datos_antropometricos.edad_metabolica || 'N/A' }} años</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Peso:</span>
                              <span class="value">{{ reporte.datos_antropometricos.peso || 'N/A' }} kg</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Peso Ajustado:</span>
                              <span class="value">{{ reporte.datos_antropometricos.peso_ajustado || 'N/A' }} kg</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Talla:</span>
                              <span class="value">{{ reporte.datos_antropometricos.talla || 'N/A' }} cm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">IMC:</span>
                              <span class="value">{{ reporte.datos_antropometricos.imc || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">C. Cintura:</span>
                              <span class="value">{{ reporte.datos_antropometricos.circunferencia_cintura || 'N/A' }} cm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">C. Cadera:</span>
                              <span class="value">{{ reporte.datos_antropometricos.circunferencia_cadera || 'N/A' }} cm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">C. Brazo:</span>
                              <span class="value">{{ reporte.datos_antropometricos.circunferencia_brazo || 'N/A' }} cm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">C. Muslo:</span>
                              <span class="value">{{ reporte.datos_antropometricos.circunferencia_muslo || 'N/A' }} cm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">C. Pantorrilla:</span>
                              <span class="value">{{ reporte.datos_antropometricos.circunferencia_pantorrilla || 'N/A' }} cm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Masa Muscular:</span>
                              <span class="value">{{ reporte.datos_antropometricos.masa_muscular || 'N/A' }} kg</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Grasa Corporal %:</span>
                              <span class="value">{{ reporte.datos_antropometricos.grasa_corporal_porcentaje || 'N/A' }}%</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Grasa Corporal:</span>
                              <span class="value">{{ reporte.datos_antropometricos.grasa_corporal || 'N/A' }} kg</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Grasa Visceral %:</span>
                              <span class="value">{{ reporte.datos_antropometricos.grasa_visceral_porcentaje || 'N/A' }}%</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Kcal Basales:</span>
                              <span class="value">{{ reporte.datos_antropometricos.kcal_basales || 'N/A' }} kcal</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Actividad Física:</span>
                              <span class="value">{{ reporte.datos_antropometricos.actividad_fisica || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Factor Act. Física:</span>
                              <span class="value">{{ reporte.datos_antropometricos.factor_actividad_fisica || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Tiempos de Comida:</span>
                              <span class="value">{{ reporte.datos_antropometricos.tiempos_comida || 'N/A' }}</span>
                            </div>
                          </div>
                        } @else {
                          <div class="no-data-message">
                            <p>No se registraron datos antropométricos en esta historia clínica</p>
                          </div>
                        }
                      </div>

                      <!-- Signos Vitales -->
                      @if (reporte.signos_vitales) {
                        <div class="detail-section">
                          <h4>Signos Vitales</h4>
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">Presión Arterial:</span>
                              <span class="value">{{ reporte.signos_vitales.presion_arterial || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Frecuencia Cardíaca:</span>
                              <span class="value">{{ reporte.signos_vitales.frecuencia_cardiaca || 'N/A' }} bpm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Frecuencia Respiratoria:</span>
                              <span class="value">{{ reporte.signos_vitales.frecuencia_respiratoria || 'N/A' }} rpm</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Temperatura:</span>
                              <span class="value">{{ reporte.signos_vitales.temperatura || 'N/A' }} °C</span>
                            </div>
                          </div>
                        </div>
                      }

                      <!-- Antecedentes -->
                      @if (reporte.antecedentes) {
                        <div class="detail-section">
                          <h4>Antecedentes</h4>
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">APF:</span>
                              <span class="value">{{ reporte.antecedentes.apf || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">APP:</span>
                              <span class="value">{{ reporte.antecedentes.app || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">APQ:</span>
                              <span class="value">{{ reporte.antecedentes.apq || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">AGO:</span>
                              <span class="value">{{ reporte.antecedentes.ago || 'N/A' }}</span>
                            </div>
                            @if (reporte.antecedentes.alergias) {
                              <div class="detail-item">
                                <span class="label">Alergias:</span>
                                <span class="value">{{ reporte.antecedentes.alergias }}</span>
                              </div>
                            }
                          </div>
                        </div>
                      }

                      <!-- Hábitos -->
                      @if (reporte.habitos) {
                        <div class="detail-section">
                          <h4>Hábitos</h4>
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">Fuma:</span>
                              <span class="value">{{ reporte.habitos.fuma || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Alcohol:</span>
                              <span class="value">{{ reporte.habitos.alcohol || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Café:</span>
                              <span class="value">{{ reporte.habitos.cafe || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Hidratación:</span>
                              <span class="value">{{ reporte.habitos.hidratacion || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Actividad Física:</span>
                              <span class="value">{{ reporte.habitos.actividad_fisica || 'N/A' }}</span>
                            </div>
                          </div>
                        </div>
                      }

                      <!-- Valores Bioquímicos -->
                      @if (reporte.valores_bioquimicos) {
                        <div class="detail-section">
                          <h4>Valores Bioquímicos</h4>
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">Glicemia:</span>
                              <span class="value">{{ reporte.valores_bioquimicos.glicemia || 'N/A' }} mg/dL</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Colesterol Total:</span>
                              <span class="value">{{ reporte.valores_bioquimicos.colesterol_total || 'N/A' }} mg/dL</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Triglicéridos:</span>
                              <span class="value">{{ reporte.valores_bioquimicos.trigliceridos || 'N/A' }} mg/dL</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">HDL:</span>
                              <span class="value">{{ reporte.valores_bioquimicos.hdl || 'N/A' }} mg/dL</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">LDL:</span>
                              <span class="value">{{ reporte.valores_bioquimicos.ldl || 'N/A' }} mg/dL</span>
                            </div>
                          </div>
                        </div>
                      }

                      <!-- Recordatorio 24h -->
                      @if (reporte.recordatorio_24h) {
                        <div class="detail-section">
                          <h4>Recordatorio 24 Horas</h4>
                          <div class="detail-grid">
                            <div class="detail-item">
                              <span class="label">Desayuno:</span>
                              <span class="value">{{ reporte.recordatorio_24h.desayuno || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Snack 1:</span>
                              <span class="value">{{ reporte.recordatorio_24h.snack1 || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Almuerzo:</span>
                              <span class="value">{{ reporte.recordatorio_24h.almuerzo || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Snack 2:</span>
                              <span class="value">{{ reporte.recordatorio_24h.snack2 || 'N/A' }}</span>
                            </div>
                            <div class="detail-item">
                              <span class="label">Cena:</span>
                              <span class="value">{{ reporte.recordatorio_24h.cena || 'N/A' }}</span>
                            </div>
                            @if (reporte.recordatorio_24h.extras) {
                              <div class="detail-item" style="grid-column: 1 / -1;">
                                <span class="label">Notas Extras:</span>
                                <span class="value">{{ reporte.recordatorio_24h.extras }}</span>
                              </div>
                            }
                          </div>
                        </div>
                      }

                      <!-- Frecuencia de Consumo -->
                      @if (reporte.frecuencia_consumo && reporte.frecuencia_consumo.length > 0) {
                        <div class="detail-section">
                          <h4>Frecuencia de Consumo</h4>
                          <div class="frecuencia-table">
                            <table>
                              <thead>
                                <tr>
                                  <th>Categoría</th>
                                  <th>Alimento</th>
                                  <th>Frecuencia</th>
                                </tr>
                              </thead>
                              <tbody>
                                @for (item of reporte.frecuencia_consumo; track $index) {
                                  <tr>
                                    <td>{{ item.categoria }}</td>
                                    <td>{{ item.alimento }}</td>
                                    <td>{{ item.frecuencia }}</td>
                                  </tr>
                                }
                              </tbody>
                            </table>
                          </div>
                        </div>
                      }
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }

      <!-- Sección de Reportes -->
      @if (showReportes()) {
        <div class="section-container">
          <div class="section-header">
            <h2>Reportes y Estadísticas</h2>
            <p>Resumen de actividad y métricas</p>
          </div>

          @if (loadingReportes()) {
            <div class="loading">
              <div class="spinner"></div>
              <p>Generando reportes...</p>
            </div>
          } @else {
            <div class="reports-grid">
              <div class="report-card">
                <h3>Resumen General</h3>
                <div class="report-stats">
                  <div class="report-stat">
                    <span class="label">Total Pacientes</span>
                    <span class="value">{{ reporteData().totalPacientes }}</span>
                  </div>
                  <div class="report-stat">
                    <span class="label">Historias Clínicas</span>
                    <span class="value">{{ reporteData().totalHistorias }}</span>
                  </div>
                  <div class="report-stat">
                    <span class="label">Historias este mes</span>
                    <span class="value">{{ reporteData().historiasUltimoMes }}</span>
                  </div>
                </div>
              </div>

              <div class="report-card">
                <h3>Distribución por Género</h3>
                <div class="gender-stats">
                  <div class="gender-item">
                    <div class="gender-bar" [style.width.%]="getGenderPercentage('masculino')">
                      <span class="gender-label">Masculino</span>
                    </div>
                    <span class="gender-count">{{ reporteData().pacientesPorGenero.masculino }}</span>
                  </div>
                  <div class="gender-item">
                    <div class="gender-bar feminine" [style.width.%]="getGenderPercentage('femenino')">
                      <span class="gender-label">Femenino</span>
                    </div>
                    <span class="gender-count">{{ reporteData().pacientesPorGenero.femenino }}</span>
                  </div>
                </div>
              </div>

              <div class="report-card">
                <h3>Actividad Reciente</h3>
                <div class="activity-list">
                  <div class="activity-item">
                    <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-12a1 1 0 10-2 0v4a1 1 0 00.293.707l2.828 2.829a1 1 0 101.415-1.415L11 9.586V6z"/>
                    </svg>
                    <div>
                      <p class="activity-title">Sistema funcionando correctamente</p>
                      <span class="activity-time">Última actualización: Hoy</span>
                    </div>
                  </div>
                  <div class="activity-item">
                    <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M9 2a1 1 0 000 2h2a1 1 0 100-2H9z"/>
                      <path fill-rule="evenodd" d="M4 5a2 2 0 012-2 3 3 0 003 3h2a3 3 0 003-3 2 2 0 012 2v11a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm3 4a1 1 0 000 2h.01a1 1 0 100-2H7zm3 0a1 1 0 000 2h3a1 1 0 100-2h-3zm-3 4a1 1 0 100 2h.01a1 1 0 100-2H7zm3 0a1 1 0 100 2h3a1 1 0 100-2h-3z"/>
                    </svg>
                    <div>
                      <p class="activity-title">Base de datos sincronizada</p>
                      <span class="activity-time">Datos actualizados</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>
      }

      <!-- Sección de Planes -->
      @if (showPlanes()) {
        <div class="section-container planes-section">
          <app-planes-alimentacion></app-planes-alimentacion>
        </div>
      }
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

    /* Secciones expandibles */
    .section-container {
      margin-top: 3rem;
      background: white;
      border-radius: 16px;
      padding: 2rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .section-header {
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 2px solid #f3f4f6;
    }

    .section-header h2 {
      margin: 0 0 0.5rem;
      font-size: 1.75rem;
      color: #1f2937;
      font-weight: 700;
    }

    .section-header p {
      margin: 0;
      color: #6b7280;
    }

    /* Loading y Empty States */
    .loading, .empty-state {
      text-align: center;
      padding: 3rem;
      color: #6b7280;
    }

    .spinner {
      width: 48px;
      height: 48px;
      margin: 0 auto 1rem;
      border: 4px solid #f3f4f6;
      border-top-color: #6366f1;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .empty-state svg {
      color: #d1d5db;
      margin-bottom: 1rem;
    }

    /* Tabla de Pacientes */
    .table-container {
      overflow-x: auto;
    }

    .patients-table {
      width: 100%;
      border-collapse: collapse;
    }

    .patients-table thead {
      background: #f9fafb;
    }

    .patients-table th {
      padding: 1rem;
      text-align: left;
      font-weight: 600;
      color: #374151;
      font-size: 0.875rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .patients-table td {
      padding: 1rem;
      border-top: 1px solid #f3f4f6;
      color: #1f2937;
    }

    .patients-table tbody tr:hover {
      background: #f9fafb;
    }

    .historias-actions {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      align-items: flex-start;
    }

    .historias-count {
      font-size: 0.813rem;
      color: #6b7280;
      font-weight: 500;
    }

    .historias-buttons {
      display: flex;
      gap: 0.375rem;
      flex-wrap: wrap;
    }

    .mini-edit-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.25rem 0.5rem;
      background: #6366f1;
      color: white;
      border: none;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .mini-edit-btn:hover {
      background: #4f46e5;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(99, 102, 241, 0.3);
    }

    .mini-edit-btn svg {
      width: 12px;
      height: 12px;
    }

    .no-historias {
      color: #9ca3af;
      font-size: 0.875rem;
      font-style: italic;
    }

    /* Reportes */
    .reports-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 1.5rem;
    }

    .report-card {
      background: #f9fafb;
      padding: 1.5rem;
      border-radius: 12px;
      border: 1px solid #e5e7eb;
    }

    .report-card h3 {
      margin: 0 0 1.5rem;
      font-size: 1.25rem;
      color: #1f2937;
      font-weight: 600;
    }

    .report-stats {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .report-stat {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem;
      background: white;
      border-radius: 8px;
    }

    .report-stat .label {
      color: #6b7280;
      font-size: 0.875rem;
    }

    .report-stat .value {
      font-size: 1.5rem;
      font-weight: 700;
      color: #6366f1;
    }

    /* Gender Stats */
    .gender-stats {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .gender-item {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .gender-bar {
      height: 36px;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      border-radius: 8px;
      display: flex;
      align-items: center;
      padding: 0 1rem;
      min-width: 100px;
      transition: width 0.3s ease;
    }

    .gender-bar.feminine {
      background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    }

    .gender-bar.other {
      background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
    }

    .gender-label {
      color: white;
      font-weight: 600;
      font-size: 0.875rem;
      white-space: nowrap;
    }

    .gender-count {
      font-weight: 700;
      color: #1f2937;
      min-width: 30px;
    }

    /* Activity List */
    .activity-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .activity-item {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      background: white;
      border-radius: 8px;
      align-items: flex-start;
    }

    .activity-item svg {
      color: #6366f1;
      flex-shrink: 0;
    }

    .activity-title {
      margin: 0 0 0.25rem;
      font-weight: 600;
      color: #1f2937;
    }

    .activity-time {
      font-size: 0.875rem;
      color: #6b7280;
    }

    /* Botón de reporte detallado */
    .detailed-report-btn {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
      box-shadow: 0 4px 12px rgba(16, 185, 129, 0.4);
      font-size: 0.875rem;
      padding: 0.75rem 1.25rem;
    }

    .detailed-report-btn:hover {
      box-shadow: 0 6px 20px rgba(16, 185, 129, 0.5);
      transform: translateY(-2px);
    }

    /* Buscador */
    .search-container {
      margin-bottom: 2rem;
    }

    .search-box {
      position: relative;
      display: flex;
      align-items: center;
      max-width: 600px;
    }

    .search-box svg {
      position: absolute;
      left: 1rem;
      color: #9ca3af;
      pointer-events: none;
    }

    .search-input {
      width: 100%;
      padding: 0.875rem 1rem 0.875rem 3rem;
      border: 2px solid #e5e7eb;
      border-radius: 12px;
      font-size: 1rem;
      transition: all 0.3s;
      outline: none;
    }

    .search-input:focus {
      border-color: #6366f1;
      box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
    }

    .clear-search {
      position: absolute;
      right: 0.75rem;
      background: #f3f4f6;
      border: none;
      border-radius: 8px;
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      transition: all 0.2s;
    }

    .clear-search:hover {
      background: #e5e7eb;
    }

    .search-results {
      margin-top: 0.75rem;
      color: #6b7280;
      font-size: 0.875rem;
    }

    /* Lista de pacientes detallados */
    .detailed-patients-list {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .detailed-patient-card {
      background: white;
      border: 1px solid #e5e7eb;
      border-radius: 12px;
      overflow: hidden;
      transition: box-shadow 0.3s;
    }

    .detailed-patient-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .patient-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      background: #f9fafb;
      border-bottom: 1px solid #e5e7eb;
      cursor: pointer;
    }

    .patient-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .edit-patient-data-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 1px 3px rgba(16, 185, 129, 0.2);
    }

    .edit-patient-data-btn:hover {
      background: #059669;
      transform: translateY(-1px);
      box-shadow: 0 4px 8px rgba(16, 185, 129, 0.3);
    }

    .edit-patient-data-btn svg {
      width: 16px;
      height: 16px;
    }

    .delete-patient-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1rem;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      box-shadow: 0 1px 3px rgba(239, 68, 68, 0.2);
    }

    .delete-patient-btn:hover {
      background: #dc2626;
      transform: translateY(-1px);
      box-shadow: 0 4px 8px rgba(239, 68, 68, 0.3);
    }

    .delete-patient-btn svg {
      width: 16px;
      height: 16px;
    }

    .patient-info h3 {
      margin: 0 0 0.5rem;
      font-size: 1.25rem;
      color: #1f2937;
      font-weight: 600;
    }

    .patient-meta {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .badge {
      display: inline-block;
      padding: 0.25rem 0.75rem;
      background: #e0e7ff;
      color: #4f46e5;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 600;
    }

    .expand-btn {
      background: none;
      border: none;
      cursor: pointer;
      padding: 0.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 8px;
      transition: all 0.3s;
    }

    .expand-btn:hover {
      background: #e5e7eb;
    }

    .expand-btn svg {
      transition: transform 0.3s;
    }

    .patient-details {
      padding: 1.5rem;
      animation: slideDown 0.3s ease-out;
    }

    @keyframes slideDown {
      from {
        opacity: 0;
        max-height: 0;
      }
      to {
        opacity: 1;
        max-height: 2000px;
      }
    }

    .detail-section {
      margin-bottom: 2rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .detail-section:last-child {
      border-bottom: none;
      margin-bottom: 0;
    }

    .detail-section h4 {
      margin: 0 0 1rem;
      font-size: 1.125rem;
      color: #1f2937;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .detail-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
      gap: 1rem;
    }

    .detail-item {
      background: #f9fafb;
      padding: 0.75rem 1rem;
      border-radius: 8px;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .detail-item .label {
      font-size: 0.75rem;
      color: #6b7280;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .detail-item .value {
      font-size: 1rem;
      color: #1f2937;
      font-weight: 500;
    }

    .no-data-message {
      background: #fef3c7;
      border: 1px solid #fbbf24;
      border-radius: 8px;
      padding: 1rem;
      text-align: center;
    }

    .no-data-message p {
      margin: 0;
      color: #92400e;
      font-size: 0.875rem;
      font-weight: 500;
    }

    /* Historias clínicas */
    .historia-card {
      background: #f9fafb;
      border-radius: 8px;
      padding: 1rem;
      margin-bottom: 1rem;
      border-left: 4px solid #6366f1;
    }

    .historia-card:last-child {
      margin-bottom: 0;
    }

    .historia-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.75rem;
      padding-bottom: 0.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .historia-header .fecha {
      font-weight: 600;
      color: #6366f1;
      font-size: 0.875rem;
    }

    .historia-buttons {
      display: flex;
      gap: 0.5rem;
    }

    .edit-historia-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      background: #6366f1;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.813rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .edit-historia-btn:hover {
      background: #4f46e5;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(99, 102, 241, 0.3);
    }

    .print-historia-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.813rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .print-historia-btn:hover {
      background: #059669;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);
    }

    .delete-historia-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.813rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .delete-historia-btn:hover {
      background: #dc2626;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(239, 68, 68, 0.3);
    }

    .edit-historia-btn svg {
      width: 14px;
      height: 14px;
    }

    .plan-nutricional-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.75rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.813rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .plan-nutricional-btn:hover {
      background: #059669;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);
    }

    .plan-nutricional-btn svg {
      width: 14px;
      height: 14px;
    }

    .plan-nutricional-container {
      margin-top: 1rem;
      border-top: 1px solid #e5e7eb;
      padding-top: 1rem;
    }

    .historia-body {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .historia-item {
      font-size: 0.875rem;
      color: #4b5563;
      line-height: 1.5;
    }

    .historia-item strong {
      color: #1f2937;
      font-weight: 600;
    }

    /* Estilos para secciones completas */
    .detail-item.full-width {
      grid-column: 1 / -1;
    }

    /* Recordatorio 24h */
    .recordatorio-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1rem;
    }

    .recordatorio-item {
      background: #f9fafb;
      padding: 1rem;
      border-radius: 8px;
      border-left: 3px solid #6366f1;
    }

    .recordatorio-item strong {
      display: block;
      color: #1f2937;
      font-weight: 600;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .recordatorio-item p {
      margin: 0;
      color: #4b5563;
      font-size: 0.875rem;
      line-height: 1.6;
    }

    /* Tabla de frecuencia de consumo */
    .frecuencia-table {
      overflow-x: auto;
      background: white;
      border-radius: 8px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .frecuencia-table table {
      width: 100%;
      border-collapse: collapse;
    }

    .frecuencia-table thead {
      background: #f9fafb;
    }

    .frecuencia-table th {
      padding: 0.75rem 1rem;
      text-align: left;
      font-weight: 600;
      color: #374151;
      font-size: 0.75rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      border-bottom: 2px solid #e5e7eb;
    }

    .frecuencia-table td {
      padding: 0.75rem 1rem;
      color: #1f2937;
      font-size: 0.875rem;
      border-bottom: 1px solid #f3f4f6;
    }

    .frecuencia-table tbody tr:hover {
      background: #f9fafb;
    }

    .frecuencia-table tbody tr:last-child td {
      border-bottom: none;
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

      .section-container {
        padding: 1.5rem;
      }

      .reports-grid {
        grid-template-columns: 1fr;
      }

      .patients-table {
        font-size: 0.875rem;
      }

      .patients-table th,
      .patients-table td {
        padding: 0.75rem 0.5rem;
      }

      .detail-grid {
        grid-template-columns: 1fr;
      }

      .patient-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .detailed-report-btn {
        width: 100%;
        justify-content: center;
      }
    }

    /* Estilos para impresión */
    @media print {
      /* Forzar que la impresión empiece desde arriba sin espacios */
      html {
        margin: 0 !important;
        padding: 0 !important;
      }

      body {
        margin: 0 !important;
        padding: 0 !important;
        background: white !important;
        position: relative !important;
      }

      /* Ocultar TODOS los elementos del dashboard excepto el contenido a imprimir */
      .dashboard-header,
      .stats-grid,
      .action-cards,
      .quick-actions,
      .section-container:not(.detailed-report) {
        display: none !important;
        position: absolute !important;
        left: -9999px !important;
        visibility: hidden !important;
      }

      /* Mostrar los planes cuando se imprime desde su propio flujo */
      :host-context(.printing-plan) .section-container.planes-section {
        display: block !important;
        position: static !important;
        left: auto !important;
        visibility: visible !important;
      }

      app-navbar,
      .navbar {
        display: none !important;
        position: absolute !important;
        left: -9999px !important;
        visibility: hidden !important;
      }

      /* Ocultar elementos dentro del reporte */
      .search-container,
      .expand-btn,
      .edit-patient-data-btn,
      .edit-historia-btn,
      .print-historia-btn,
      .delete-historia-btn,
      .action-btn,
      .print-btn,
      .patient-actions,
      .section-header h2,
      .section-header p,
      .search-results {
        display: none !important;
      }

      /* Configuración de página */
      @page {
        margin: 0.5cm;
        size: letter;
      }

      /* Contenedor principal pegado arriba */
      .dashboard-container {
        position: static !important;
        margin: 0 !important;
        padding: 0 !important;
      }

      .detailed-report {
        position: relative !important;
        margin: 0 !important;
        padding: 0 !important;
        top: 0 !important;
      }

      .section-container {
        margin: 0 !important;
        padding: 0 !important;
      }

      .section-header {
        display: none !important;
      }

      .detailed-patients-list {
        margin: 0 !important;
        padding: 0 !important;
      }

      /* Contenedor principal - sin saltos de página */
      .section-container {
        box-shadow: none !important;
        border: none !important;
        page-break-before: avoid !important;
        page-break-inside: avoid;
        margin: 0 !important;
        padding: 0 !important;
      }

      /* Tarjeta de paciente - primera sin salto previo */
      .detailed-patient-card {
        page-break-inside: avoid;
        page-break-after: auto;
        border: 2px solid #333 !important;
        margin: 0 0 1rem 0;
        padding: 0.5rem;
        box-shadow: none !important;
      }

      .detailed-patient-card:first-child {
        page-break-before: avoid !important;
        margin-top: 0 !important;
      }

      /* Header del paciente */
      .patient-header {
        border-bottom: 2px solid #333;
        padding-bottom: 0.5rem;
        margin-bottom: 0.5rem;
      }

      .patient-info h3 {
        font-size: 1.25rem;
        margin: 0 0 0.375rem 0;
        color: #000;
      }

      .patient-meta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.375rem;
      }

      .badge {
        border: 1px solid #666;
        padding: 0.125rem 0.375rem;
        font-size: 0.75rem;
      }

      /* Forzar la visualización de todos los detalles */
      .patient-details {
        display: block !important;
      }

      /* Secciones de detalles */
      .detail-section {
        page-break-inside: avoid;
        margin-bottom: 0.75rem;
        border: 1px solid #ddd;
        padding: 0.5rem;
      }

      .detail-section h4 {
        font-size: 0.95rem;
        margin-bottom: 0.5rem;
        color: #000;
        border-bottom: 1px solid #333;
        padding-bottom: 0.25rem;
      }

      .detail-grid {
        display: grid;
        grid-template-columns: repeat(2, 1fr);
        gap: 0.5rem;
      }

      .detail-item {
        display: flex;
        flex-direction: column;
        gap: 0.125rem;
      }

      .detail-item .label {
        font-weight: 600;
        color: #333;
        font-size: 0.75rem;
      }

      .detail-item .value {
        color: #000;
        font-size: 0.75rem;
      }

      /* Historia clínica */
      .historia-card {
        border: 1px solid #999;
        padding: 0.5rem;
        margin-bottom: 0.5rem;
      }

      .historia-header {
        margin-bottom: 0.375rem;
        border-bottom: 1px solid #ddd;
        padding-bottom: 0.375rem;
      }

      .historia-header .fecha {
        font-weight: 600;
        font-size: 0.875rem;
      }

      .historia-item {
        margin-bottom: 0.375rem;
        font-size: 0.75rem;
      }

      /* Tabla de frecuencia de consumo - Optimizada para caber en una página */
      .frecuencia-table {
        page-break-before: auto;
      }

      .frecuencia-table table {
        width: 100%;
        border-collapse: collapse;
        margin-top: 0.5rem;
        font-size: 0.7rem;
      }

      .frecuencia-table th,
      .frecuencia-table td {
        border: 1px solid #333;
        padding: 0.25rem 0.375rem;
        text-align: left;
        font-size: 0.7rem;
        line-height: 1.2;
      }

      .frecuencia-table th {
        background-color: #f0f0f0;
        font-weight: 600;
      }

      .frecuencia-table thead {
        background-color: #e0e0e0;
      }

      /* Permitir que la tabla se divida si es muy larga */
      .frecuencia-table tbody {
        page-break-inside: auto;
      }

      .frecuencia-table tr {
        page-break-inside: avoid;
        page-break-after: auto;
      }

      /* Ajustes generales */
      .section-header {
        margin-bottom: 1rem;
      }

      .section-header h2 {
        font-size: 1.75rem;
        margin-bottom: 0.25rem;
        color: #000;
      }

      .section-header p {
        font-size: 0.875rem;
        color: #666;
      }

      body {
        background: white !important;
        color: #000 !important;
      }

      /* Asegurar que no se corten las secciones */
      .detail-section,
      .historia-card,
      .frecuencia-table {
        page-break-inside: avoid;
      }
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
    }

    .header-content {
      flex: 1;
    }

    .print-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.625rem 1rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      white-space: nowrap;
    }

    .print-btn:hover {
      background: #059669;
      transform: translateY(-1px);
      box-shadow: 0 4px 6px rgba(16, 185, 129, 0.25);
    }

    .print-btn:active {
      transform: translateY(0);
    }
  `]
})
export class DashboardComponent implements OnInit {
  private apiUrl = 'http://localhost:5000/api';
  
  showPacientes = signal(false);
  showReportes = signal(false);
  showReporteDetallado = signal(false);
  showPlanes = signal(false);
  loadingPacientes = signal(false);
  loadingReportes = signal(false);
  loadingDetallado = signal(false);
  
  pacientes = signal<Paciente[]>([]);
  pacientesDetallados = signal<PacienteDetallado[]>([]);
  expandedPacientes = signal<Set<string>>(new Set());
  searchTerm = signal('');
  planNutricionalActivo = signal<string | null>(null); // historia_id del plan que se está mostrando
  
  reporteData = signal<ReporteData>({
    totalPacientes: 0,
    totalHistorias: 0,
    pacientesPorGenero: { masculino: 0, femenino: 0 },
    historiasUltimoMes: 0,
    totalPlanes: 0
  });

  pacientesFiltrados = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) {
      return this.pacientesDetallados();
    }
    
    return this.pacientesDetallados().filter(p => 
      (p.nombre && p.nombre.toLowerCase().includes(term)) ||
      (p.numero_cedula && p.numero_cedula.toLowerCase().includes(term)) ||
      (p.fecha_consulta && p.fecha_consulta.toLowerCase().includes(term))
    );
  });

  constructor(
    public authService: AuthService,
    private http: HttpClient,
    private router: Router,
    private nutritionService: NutritionService,
    private editDataService: EditDataService
  ) {}

  ngOnInit() {
    // Cargar reportes automáticamente al iniciar
    this.loadReportes();
  }

  togglePacientes() {
    this.showPacientes.update(v => !v);
    if (this.showPacientes() && this.pacientes().length === 0) {
      this.loadPacientes();
    }
  }

  toggleReportes() {
    this.showReportes.update(v => !v);
    if (this.showReportes()) {
      this.loadReportes();
    }
  }

  togglePlanes() {
    this.showPlanes.update(v => !v);
  }

  toggleReporteDetallado() {
    this.showReporteDetallado.update(v => !v);
    if (this.showReporteDetallado() && this.pacientesDetallados().length === 0) {
      this.loadPacientesDetallados();
    }
  }

  togglePacienteExpanded(reporteId: string) {
    this.expandedPacientes.update(set => {
      const newSet = new Set(set);
      if (newSet.has(reporteId)) {
        newSet.delete(reporteId);
      } else {
        newSet.add(reporteId);
      }
      return newSet;
    });
  }

  isPacienteExpanded(reporteId: string): boolean {
    return this.expandedPacientes().has(reporteId);
  }

  togglePlanNutricional(historiaId: string) {
    if (this.planNutricionalActivo() === historiaId) {
      this.planNutricionalActivo.set(null);
    } else {
      this.planNutricionalActivo.set(historiaId);
    }
  }

  isPlanNutricionalActivo(historiaId: string): boolean {
    return this.planNutricionalActivo() === historiaId;
  }

  onSearchChange() {
    // El computed ya maneja el filtrado automáticamente
  }

  clearSearch() {
    this.searchTerm.set('');
  }

  loadPacientes() {
    this.loadingPacientes.set(true);
    
    this.http.get<Paciente[]>(`${this.apiUrl}/nutrition/pacientes`).subscribe({
      next: (data) => {
        this.pacientes.set(data);
        this.loadingPacientes.set(false);
      },
      error: (error) => {
        console.error('Error al cargar pacientes:', error);
        this.loadingPacientes.set(false);
        this.pacientes.set([]);
      }
    });
  }

  loadPacientesDetallados() {
    this.loadingDetallado.set(true);
    
    this.http.get<PacienteDetallado[]>(`${this.apiUrl}/nutrition/pacientes/detallados`).subscribe({
      next: (data) => {
        this.pacientesDetallados.set(data);
        this.loadingDetallado.set(false);
      },
      error: (error) => {
        console.error('Error al cargar pacientes detallados:', error);
        this.loadingDetallado.set(false);
        this.pacientesDetallados.set([]);
      }
    });
  }

  loadReportes() {
    this.loadingReportes.set(true);
    
    this.http.get<ReporteData>(`${this.apiUrl}/nutrition/reportes`).subscribe({
      next: (data) => {
        this.reporteData.set(data);
        this.loadingReportes.set(false);
      },
      error: (error) => {
        console.error('Error al cargar reportes:', error);
        this.loadingReportes.set(false);
        // Mostrar datos vacíos en lugar de generar datos falsos
        this.reporteData.set({
          totalPacientes: 0,
          totalHistorias: 0,
          pacientesPorGenero: { masculino: 0, femenino: 0 },
          historiasUltimoMes: 0,
          totalPlanes: 0
        });
      }
    });
  }

  getGenderPercentage(gender: 'masculino' | 'femenino'): number {
    const total = this.reporteData().pacientesPorGenero.masculino +
                  this.reporteData().pacientesPorGenero.femenino;
    
    if (total === 0) return 0;
    
    return (this.reporteData().pacientesPorGenero[gender] / total) * 100;
  }

  editarHistoria(historiaId: string) {
    console.log('====== EDITAR HISTORIA CLÍNICA ======');
    console.log('Historia ID:', historiaId);
    
    // Cargar los datos de la historia y navegar al formulario principal
    this.nutritionService.getHistoriaClinica(historiaId).subscribe({
      next: (data: any) => {
        console.log('Datos de historia recibidos:', data);
        this.editDataService.setEditHistoria(historiaId, data);
        console.log('EditContext set - navegando a /history');
        this.router.navigate(['/history']);
      },
      error: (error) => {
        console.error('Error cargando historia clínica:', error);
        alert('Error al cargar la historia clínica para edición');
      }
    });
  }

  editarDatosPaciente(pacienteId: string) {
    console.log('Editar datos del paciente ID:', pacienteId);
    // Navegar al formulario principal con el ID del paciente
    // El componente principal se encargará de cargar los datos
    this.editDataService.setEditPaciente(pacienteId);
    this.router.navigate(['/history']);
  }

  eliminarHistoria(historiaId: string, pacienteNombre: string) {
    const confirmar = confirm(
      `⚠️ ELIMINAR HISTORIA CLÍNICA ⚠️\n\n` +
      `Esta acción eliminará ÚNICAMENTE esta historia clínica.\n` +
      `El paciente "${pacienteNombre}" NO será eliminado.\n\n` +
      `Se borrarán:\n` +
      `• Esta consulta y diagnóstico\n` +
      `• Datos antropométricos de esta fecha\n` +
      `• Valores bioquímicos registrados\n` +
      `• Recordatorio 24h y frecuencia de consumo\n\n` +
      `¿Deseas continuar?\n` +
      `(Esta acción NO se puede deshacer)`
    );

    if (!confirmar) return;

    console.log(`[ELIMINAR HISTORIA] ID: ${historiaId}, Paciente: ${pacienteNombre}`);
    
    this.nutritionService.deleteHistoriaClinica(historiaId).subscribe({
      next: (response: any) => {
        console.log('✅ Historia clínica eliminada exitosamente:', response);
        alert(
          `✅ Historia clínica eliminada correctamente.\n\n` +
          `Paciente: ${response.paciente}\n` +
          `Fecha de consulta: ${response.fecha || 'N/A'}\n\n` +
          `El paciente sigue registrado en el sistema.`
        );
        // Recargar los datos del dashboard
        this.loadReportes();
      },
      error: (error) => {
        console.error('❌ Error eliminando historia clínica:', error);
        const mensaje = error.error?.error || error.message || 'Error desconocido';
        alert(`❌ Error al eliminar la historia clínica:\n\n${mensaje}`);
      }
    });
  }

  getSexoAbreviado(sexo: string | null | undefined): string {
    if (!sexo) return '';
    const sexoLower = sexo.toLowerCase();
    if (sexoLower.includes('f') || sexoLower.includes('mujer')) return 'F';
    if (sexoLower.includes('m') || sexoLower.includes('hombre') || sexoLower.includes('varon')) return 'M';
    return '';
  }

  eliminarPaciente(pacienteId: string, nombrePaciente: string) {
    const confirmacion = confirm(
      `¿Está seguro que desea eliminar al paciente "${nombrePaciente}"?\n\n` +
      'Esta acción eliminará:\n' +
      '- Todos los datos del paciente\n' +
      '- Todas sus historias clínicas\n' +
      '- Todos los planes nutricionales asociados\n\n' +
      'Esta acción NO se puede deshacer.'
    );

    if (!confirmacion) {
      return;
    }

    this.nutritionService.deletePaciente(pacienteId).subscribe({
      next: () => {
        alert(`Paciente "${nombrePaciente}" eliminado exitosamente`);
        // Recargar los datos
        this.loadPacientesDetallados();
        this.loadReportes();
        if (this.showPacientes()) {
          this.loadPacientes();
        }
      },
      error: (error) => {
        console.error('Error al eliminar paciente:', error);
        alert('Error al eliminar el paciente. Por favor intente nuevamente.');
      }
    });
  }

  imprimirHistoria(historiaId: string) {
    // Expandir solo este reporte antes de imprimir
    this.expandedPacientes.update(set => {
      const newSet = new Set(set);
      newSet.add(historiaId);
      return newSet;
    });
    
    // Esperar un momento para que Angular actualice el DOM con los detalles expandidos
    setTimeout(() => {
      // Ocultar temporalmente todas las tarjetas excepto la que se va a imprimir
      const cards = document.querySelectorAll('.detailed-patient-card');
      cards.forEach(card => {
        const cardElement = card as HTMLElement;
        const cardHistoriaId = cardElement.getAttribute('data-historia-id');
        if (cardHistoriaId !== historiaId) {
          cardElement.style.display = 'none';
        }
      });
      
      window.print();
      
      // Restaurar todas las tarjetas después de imprimir
      setTimeout(() => {
        cards.forEach(card => {
          (card as HTMLElement).style.display = '';
        });
      }, 500);
    }, 200);
  }
}
