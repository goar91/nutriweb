import { Component, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlanesService } from '../../services/planes.service';
import { HttpClient } from '@angular/common/http';

interface ComidaDia {
  desayuno: string;
  snack1: string;
  almuerzo: string;
  snack2: string;
  merienda: string;
}

interface SemanaPlan {
  lunes: ComidaDia;
  martes: ComidaDia;
  miercoles: ComidaDia;
  jueves: ComidaDia;
  viernes: ComidaDia;
  sabado: ComidaDia;
  domingo: ComidaDia;
}

@Component({
  selector: 'app-planes-alimentacion',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="planes-container">
      <div class="planes-header">
        <h2>Plan de Alimentación - Cuatro Semanas</h2>
        <div class="header-actions">
          <div class="historia-selector">
            <label for="historiaSelect">Historia Clínica:</label>
            <select 
              id="historiaSelect" 
              [(ngModel)]="historiaSeleccionada"
              (change)="onHistoriaChange()"
              class="select-historia">
              <option value="">Seleccione una historia clínica</option>
              @for (historia of historiasDisponibles(); track historia.id) {
                <option [value]="historia.id">
                  {{ historia.paciente_nombre }} - {{ historia.fecha_consulta }} ({{ historia.paciente_cedula }})
                </option>
              }
            </select>
          </div>
          <button class="btn-save" (click)="guardarPlan()" [disabled]="!historiaSeleccionada">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M7.707 10.293a1 1 0 10-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 11.586V6h5a2 2 0 012 2v7a2 2 0 01-2 2H4a2 2 0 01-2-2V8a2 2 0 012-2h5v5.586l-1.293-1.293zM9 4a1 1 0 012 0v2H9V4z"/>
            </svg>
            Guardar Plan
          </button>
          <button class="btn-secondary" (click)="limpiarPlan()">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd"/>
            </svg>
            Limpiar
          </button>
          <button class="btn-view" (click)="toggleVerPlanes()" [disabled]="!historiaSeleccionada">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
              <path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/>
              <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/>
            </svg>
            {{ mostrarPlanesGuardados() ? 'Ocultar Planes' : 'Ver Planes Guardados' }}
          </button>
        </div>
      </div>

      @if (mostrarPlanesGuardados()) {
        <div class="planes-guardados-section">
          <h3>Planes Guardados</h3>
          @if (cargandoPlanes) {
            <div class="loading">Cargando planes...</div>
          } @else if (planesGuardados().length === 0) {
            <div class="no-planes">No hay planes guardados para esta historia clínica</div>
          } @else {
            <div class="planes-list">
              @for (plan of planesGuardados(); track plan.id) {
                <div class="plan-item">
                  <div class="plan-header">
                    <div class="plan-info">
                      <strong>Plan ID:</strong> {{ plan.id }}
                      <span class="separator">|</span>
                      <strong>Fecha Inicio:</strong> {{ plan.fecha_inicio | date: 'dd/MM/yyyy' }}
                      @if (plan.fecha_fin) {
                        <span class="separator">|</span>
                        <strong>Fecha Fin:</strong> {{ plan.fecha_fin | date: 'dd/MM/yyyy' }}
                      }
                    </div>
                    <div class="plan-actions">
                      <button class="btn-load" (click)="cargarPlan(plan.id)" title="Cargar este plan">
                        <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd"/>
                        </svg>
                        Cargar
                      </button>
                      <button class="btn-delete" (click)="eliminarPlan(plan.id)" title="Eliminar este plan">
                        <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                          <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/>
                        </svg>
                        Eliminar
                      </button>
                    </div>
                  </div>
                  @if (plan.objetivo) {
                    <div class="plan-detail"><strong>Objetivo:</strong> {{ plan.objetivo }}</div>
                  }
                  @if (plan.observaciones) {
                    <div class="plan-detail"><strong>Observaciones:</strong> {{ plan.observaciones }}</div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }

      <div class="semanas-tabs">
        <div class="tab-group">
          <button 
            class="tab-btn" 
            [class.active]="semanaActual() === 1"
            (click)="cambiarSemana(1)">
            Semana 1
          </button>
          <button 
            class="tab-btn" 
            [class.active]="semanaActual() === 2"
            (click)="cambiarSemana(2)">
            Semana 2
          </button>
          <button 
            class="tab-btn" 
            [class.active]="semanaActual() === 3"
            (click)="cambiarSemana(3)">
            Semana 3
          </button>
          <button 
            class="tab-btn" 
            [class.active]="semanaActual() === 4"
            (click)="cambiarSemana(4)">
            Semana 4
          </button>
        </div>
        <div class="semanas-actions">
          <button 
            class="btn-print-semana btn-print-all" 
            (click)="mostrarModalImpresion.set(true)"
            [disabled]="cargando || !historiaSeleccionada">
            <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M5 4v3H4a2 2 0 00-2 2v3a2 2 0 002 2h1v2a2 2 0 002 2h6a2 2 0 002-2v-2h1a2 2 0 002-2V9a2 2 0 00-2-2h-1V4a2 2 0 00-2-2H7a2 2 0 00-2 2zm8 0H7v3h6V4zm0 8H7v4h6v-4z" clip-rule="evenodd"/>
              <path d="M6 14h8v2H6z"/>
            </svg>
            Imprimir Semanas
          </button>
          <button 
            class="btn-delete"
            (click)="eliminarPlanActual()"
            [disabled]="!planActualId || cargando"
            title="Eliminar plan actual">
            <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd"/>
            </svg>
            Eliminar Plan
          </button>
        </div>
      </div>

      @if (mostrarModalImpresion()) {
        <div class="modal-overlay" (click)="mostrarModalImpresion.set(false)">
          <div class="modal-content" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h3>Seleccionar Semanas para Imprimir</h3>
              <button class="modal-close" (click)="mostrarModalImpresion.set(false)">
                <svg width="24" height="24" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"/>
                </svg>
              </button>
            </div>
            <div class="modal-body">
              <p class="modal-description">Selecciona las semanas que deseas incluir en la impresión:</p>
              <div class="semanas-checkboxes">
                <label class="checkbox-item">
                  <input type="checkbox" [(ngModel)]="imprimirSemana1" />
                  <span>Semana 1</span>
                </label>
                <label class="checkbox-item">
                  <input type="checkbox" [(ngModel)]="imprimirSemana2" />
                  <span>Semana 2</span>
                </label>
                <label class="checkbox-item">
                  <input type="checkbox" [(ngModel)]="imprimirSemana3" />
                  <span>Semana 3</span>
                </label>
                <label class="checkbox-item">
                  <input type="checkbox" [(ngModel)]="imprimirSemana4" />
                  <span>Semana 4</span>
                </label>
              </div>
            </div>
            <div class="modal-footer">
              <button class="btn-secondary" (click)="mostrarModalImpresion.set(false)">Cancelar</button>
              <button class="btn-primary" (click)="confirmarImpresion()" [disabled]="!tieneSemanaSeleccionada()">
                <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M5 4v3H4a2 2 0 00-2 2v3a2 2 0 002 2h1v2a2 2 0 002 2h6a2 2 0 002-2v-2h1a2 2 0 002-2V9a2 2 0 00-2-2h-1V4a2 2 0 00-2-2H7a2 2 0 00-2 2zm8 0H7v3h6V4zm0 8H7v4h6v-4z" clip-rule="evenodd"/>
                </svg>
                Imprimir
              </button>
            </div>
          </div>
        </div>
      }

      <div class="plan-grid">
        @for (dia of diasSemana; track dia.key) {
          <div class="dia-card">
            <div class="dia-header">
              <h3>{{ dia.nombre }}</h3>
            </div>
            <div class="comidas-list">
              <div class="comida-item">
                <label>
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M3 1a1 1 0 000 2h1.22l.305 1.222a.997.997 0 00.01.042l1.358 5.43-.893.892C3.74 11.846 4.632 14 6.414 14H15a1 1 0 000-2H6.414l1-1H14a1 1 0 00.894-.553l3-6A1 1 0 0017 3H6.28l-.31-1.243A1 1 0 005 1H3zM16 16.5a1.5 1.5 0 11-3 0 1.5 1.5 0 013 0zM6.5 18a1.5 1.5 0 100-3 1.5 1.5 0 000 3z"/>
                  </svg>
                  Desayuno
                </label>
                <textarea 
                  [(ngModel)]="getPlanActual()[dia.key].desayuno"
                  placeholder="Ej: Café con leche, tostadas integrales con aguacate..."
                  rows="2"></textarea>
              </div>

              <div class="comida-item">
                <label>
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 004 14h12a1 1 0 00.707-1.707L16 11.586V8a6 6 0 00-6-6zM10 18a3 3 0 01-3-3h6a3 3 0 01-3 3z"/>
                  </svg>
                  Snack
                </label>
                <textarea 
                  [(ngModel)]="getPlanActual()[dia.key].snack1"
                  placeholder="Ej: Yogurt griego con frutas..."
                  rows="2"></textarea>
              </div>

              <div class="comida-item">
                <label>
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M6 2a2 2 0 00-2 2v12a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2H6zm1 2a1 1 0 000 2h6a1 1 0 100-2H7zm6 7a1 1 0 011 1v3a1 1 0 11-2 0v-3a1 1 0 011-1zm-3 3a1 1 0 100 2h.01a1 1 0 100-2H10zm-4 1a1 1 0 011-1h.01a1 1 0 110 2H7a1 1 0 01-1-1zm1-4a1 1 0 100 2h.01a1 1 0 100-2H7zm2 1a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zm4-4a1 1 0 100 2h.01a1 1 0 100-2H13zM9 9a1 1 0 011-1h.01a1 1 0 110 2H10a1 1 0 01-1-1zM7 8a1 1 0 000 2h.01a1 1 0 000-2H7z" clip-rule="evenodd"/>
                  </svg>
                  Almuerzo
                </label>
                <textarea 
                  [(ngModel)]="getPlanActual()[dia.key].almuerzo"
                  placeholder="Ej: Pollo a la plancha con ensalada mixta y arroz integral..."
                  rows="2"></textarea>
              </div>

              <div class="comida-item">
                <label>
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 004 14h12a1 1 0 00.707-1.707L16 11.586V8a6 6 0 00-6-6zM10 18a3 3 0 01-3-3h6a3 3 0 01-3 3z"/>
                  </svg>
                  Snack
                </label>
                <textarea 
                  [(ngModel)]="getPlanActual()[dia.key].snack2"
                  placeholder="Ej: Frutos secos, manzana..."
                  rows="2"></textarea>
              </div>

              <div class="comida-item">
                <label>
                  <svg width="16" height="16" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                  </svg>
                  Merienda
                </label>
                <textarea 
                  [(ngModel)]="getPlanActual()[dia.key].merienda"
                  placeholder="Ej: Salmón al horno con vegetales al vapor..."
                  rows="2"></textarea>
              </div>
            </div>
          </div>
        }
      </div>

      @if (imprimiendoAmbasSemanas()) {
        <div class="print-all-container">
          @for (semana of semanasParaImprimir(); track semana) {
            @let plan = getPlanSemana(semana);
            <div class="print-week">
              <div class="print-week-header">
                <h3>Semana {{ semana }}</h3>
              </div>
              <div class="plan-grid print-grid">
                @for (dia of diasSemana; track dia.key) {
                  <div class="dia-card">
                    <div class="dia-header">
                      <h3>{{ dia.nombre }}</h3>
                    </div>
                    <div class="comidas-list">
                      <div class="comida-item">
                        <label>Desayuno</label>
                        <div class="comida-text">{{ plan?.[dia.key]?.desayuno || 'N/A' }}</div>
                      </div>
                      <div class="comida-item">
                        <label>Snack</label>
                        <div class="comida-text">{{ plan?.[dia.key]?.snack1 || 'N/A' }}</div>
                      </div>
                      <div class="comida-item">
                        <label>Almuerzo</label>
                        <div class="comida-text">{{ plan?.[dia.key]?.almuerzo || 'N/A' }}</div>
                      </div>
                      <div class="comida-item">
                        <label>Snack</label>
                        <div class="comida-text">{{ plan?.[dia.key]?.snack2 || 'N/A' }}</div>
                      </div>
                      <div class="comida-item">
                        <label>Merienda</label>
                        <div class="comida-text">{{ plan?.[dia.key]?.merienda || 'N/A' }}</div>
                      </div>
                    </div>
                  </div>
                }
              </div>
            </div>
          }
        </div>
      }

      @if (mensajeGuardado()) {
        <div class="mensaje-exito">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
          </svg>
          Plan guardado exitosamente
        </div>
      }
    </div>
  `,
  styles: [`
    .planes-container {
      padding: 2rem;
      max-width: 1600px;
      margin: 0 auto;
    }

    .planes-header {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin-bottom: 2rem;
      padding-bottom: 1rem;
      border-bottom: 2px solid #e0e0e0;
    }

    .planes-header h2 {
      margin: 0;
      color: #333;
      font-size: 1.75rem;
    }

    .header-actions {
      display: flex;
      gap: 1rem;
      align-items: center;
      flex-wrap: wrap;
    }

    .historia-selector {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex: 1;
      min-width: 300px;
    }

    .historia-selector label {
      font-weight: 600;
      color: #555;
      white-space: nowrap;
    }

    .select-historia {
      flex: 1;
      padding: 0.75rem;
      border: 2px solid #e0e0e0;
      border-radius: 0.5rem;
      font-size: 0.95rem;
      background: white;
      cursor: pointer;
      transition: border-color 0.3s;
    }

    .select-historia:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .btn-save, .btn-secondary {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      border: none;
      border-radius: 0.5rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
    }

    .btn-save {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .btn-save:hover {
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
    }

    .btn-secondary {
      background: white;
      color: #667eea;
      border: 2px solid #667eea;
    }

    .btn-secondary:hover {
      background: #f5f7ff;
    }

    .semanas-tabs {
      display: flex;
      gap: 2rem;
      margin-bottom: 2rem;
      align-items: center;
    }

    .tab-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .semanas-actions {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .tab-btn {
      padding: 0.75rem 2rem;
      background: white;
      border: 2px solid #e0e0e0;
      border-radius: 0.5rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
      color: #666;
    }

    .tab-btn.active {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border-color: transparent;
    }

    .tab-btn:not(.active):hover {
      border-color: #667eea;
      color: #667eea;
    }

    .btn-print-semana {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.5rem 0.875rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-print-semana:hover {
      background: #059669;
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(16, 185, 129, 0.3);
    }

    .btn-print-semana svg {
      flex-shrink: 0;
    }

    .btn-print-all {
      background: linear-gradient(135deg, #10b981 0%, #0ea5e9 100%);
    }

    .btn-print-all:hover {
      background: linear-gradient(135deg, #0ea5e9 0%, #10b981 100%);
    }

    .plan-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 1.5rem;
    }

    .dia-card {
      background: white;
      border-radius: 0.75rem;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
      overflow: hidden;
      transition: transform 0.3s, box-shadow 0.3s;
    }

    .dia-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
    }

    .dia-header {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 1rem;
      text-align: center;
    }

    .dia-header h3 {
      margin: 0;
      font-size: 1.1rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .comidas-list {
      padding: 1.25rem;
    }

    .comida-item {
      margin-bottom: 1rem;
    }

    .comida-item:last-child {
      margin-bottom: 0;
    }

    .comida-item label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-weight: 600;
      color: #555;
      margin-bottom: 0.5rem;
      font-size: 0.9rem;
    }

    .comida-item label svg {
      color: #667eea;
    }

    .comida-item textarea {
      width: 100%;
      padding: 0.75rem;
      border: 2px solid #e0e0e0;
      border-radius: 0.5rem;
      font-size: 0.9rem;
      font-family: inherit;
      resize: vertical;
      transition: border-color 0.3s;
    }

    .comida-item textarea:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .comida-item textarea::placeholder {
      color: #999;
      font-style: italic;
    }

    .print-all-container {
      display: flex;
      flex-direction: column;
      gap: 2rem;
      margin-top: 1.5rem;
    }

    .print-week-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .print-grid .dia-card {
      box-shadow: none;
      border: 1px solid #e5e7eb;
    }

    .comida-text {
      padding: 0.65rem;
      border: 1px solid #e5e7eb;
      border-radius: 0.5rem;
      min-height: 48px;
      background: #f9fafb;
      white-space: pre-wrap;
    }

    .mensaje-exito {
      position: fixed;
      bottom: 2rem;
      right: 2rem;
      background: #4caf50;
      color: white;
      padding: 1rem 1.5rem;
      border-radius: 0.5rem;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      box-shadow: 0 4px 12px rgba(76, 175, 80, 0.3);
      animation: slideIn 0.3s ease-out;
      font-weight: 600;
    }

    @keyframes slideIn {
      from {
        transform: translateX(400px);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    /* Responsive Design - Tablets */
    @media (max-width: 1024px) {
      .planes-container {
        padding: 1.5rem;
      }

      .plan-grid {
        grid-template-columns: repeat(2, 1fr);
        gap: 1rem;
      }
    }

    /* Responsive Design - Mobile */
    @media (max-width: 768px) {
      .planes-container {
        padding: 1rem;
      }

      .planes-header {
        gap: 1rem;
      }

      .header-actions {
        width: 100%;
        flex-direction: column;
        gap: 0.75rem;
      }

      .historia-selector {
        width: 100%;
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }

      .select-historia {
        width: 100%;
      }

      .btn-save, .btn-secondary, .btn-view {
        width: 100%;
        justify-content: center;
        padding: 0.75rem 1rem;
      }

      .plan-grid {
        grid-template-columns: 1fr;
        gap: 1rem;
      }

      .dia-card {
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }

      .comida-item textarea {
        font-size: 0.875rem;
      }

      .mensaje-exito {
        bottom: 1rem;
        right: 1rem;
        left: 1rem;
        font-size: 0.875rem;
      }

      .tab-group {
        width: 100%;
      }

      .tab-btn {
        flex: 1;
        font-size: 0.875rem;
      }

      .semanas-actions {
        width: 100%;
        flex-direction: column;
        align-items: stretch;
      }

      .btn-print-semana {
        width: 100%;
        justify-content: center;
      }

      .semanas-actions .btn-delete {
        width: 100%;
        justify-content: center;
      }
    }

    /* Responsive Design - Small Mobile */
    @media (max-width: 480px) {
      .planes-container {
        padding: 0.75rem;
      }

      .planes-header h2 {
        font-size: 1.5rem;
      }

      .dia-card {
        border-radius: 8px;
      }

      .dia-header {
        padding: 0.75rem;
      }

      .dia-header h3 {
        font-size: 1rem;
      }

      .comidas-list {
        padding: 1rem;
      }

      .comida-item {
        margin-bottom: 0.75rem;
      }

      .comida-item label {
        font-size: 0.8125rem;
      }

      .comida-item textarea {
        font-size: 0.8125rem;
        padding: 0.625rem;
      }

      .semanas-tabs {
        flex-direction: column;
        gap: 0.75rem;
      }
    }

    .planes-guardados-section {
      background: #f8f9fa;
      border-radius: 12px;
      padding: 1.5rem;
      margin-bottom: 2rem;
    }

    .planes-guardados-section h3 {
      margin: 0 0 1rem 0;
      color: #1f2937;
      font-size: 1.25rem;
    }

    .loading, .no-planes {
      text-align: center;
      padding: 2rem;
      color: #6b7280;
      font-style: italic;
    }

    .planes-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .plan-item {
      background: white;
      border-radius: 8px;
      padding: 1rem;
      border: 1px solid #e5e7eb;
      transition: box-shadow 0.2s;
    }

    .plan-item:hover {
      box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    }

    .plan-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.5rem;
    }

    .plan-info {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      flex-wrap: wrap;
      font-size: 0.9rem;
      color: #374151;
    }

    .plan-info .separator {
      color: #d1d5db;
    }

    .plan-detail {
      margin-top: 0.5rem;
      font-size: 0.9rem;
      color: #4b5563;
    }

    .plan-actions {
      display: flex;
      gap: 0.5rem;
    }

    .btn-load {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: #10b981;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
    }

    .btn-load:hover {
      background: #059669;
    }

    .btn-delete {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: background 0.2s;
    }

    .btn-delete:hover {
      background: #dc2626;
    }

    .btn-delete:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-view {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.25rem;
      background: #3b82f6;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-view:hover:not(:disabled) {
      background: #2563eb;
      transform: translateY(-1px);
    }

    .btn-view:disabled {
      background: #9ca3af;
      cursor: not-allowed;
      opacity: 0.6;
    }

    /* Modal de impresión */
    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      animation: fadeIn 0.2s ease-in-out;
    }

    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    .modal-content {
      background: white;
      border-radius: 12px;
      padding: 0;
      width: 90%;
      max-width: 500px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from { transform: translateY(-20px); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1.5rem;
      border-bottom: 1px solid #e5e7eb;
    }

    .modal-header h3 {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 700;
      color: #1f2937;
    }

    .modal-close {
      background: none;
      border: none;
      color: #6b7280;
      cursor: pointer;
      padding: 0.25rem;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 6px;
      transition: all 0.2s;
    }

    .modal-close:hover {
      background: #f3f4f6;
      color: #1f2937;
    }

    .modal-body {
      padding: 1.5rem;
    }

    .modal-description {
      margin: 0 0 1.25rem 0;
      color: #6b7280;
      font-size: 0.9375rem;
    }

    .semanas-checkboxes {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .checkbox-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.875rem 1rem;
      border: 2px solid #e5e7eb;
      border-radius: 8px;
      cursor: pointer;
      transition: all 0.2s;
      user-select: none;
    }

    .checkbox-item:hover {
      border-color: #667eea;
      background: #f9fafb;
    }

    .checkbox-item input[type="checkbox"] {
      width: 20px;
      height: 20px;
      cursor: pointer;
      accent-color: #667eea;
    }

    .checkbox-item span {
      font-size: 1rem;
      font-weight: 500;
      color: #374151;
    }

    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      padding: 1.5rem;
      border-top: 1px solid #e5e7eb;
      background: #f9fafb;
      border-bottom-left-radius: 12px;
      border-bottom-right-radius: 12px;
    }

    .btn-primary {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.75rem 1.5rem;
      background: #667eea;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-primary:hover:not(:disabled) {
      background: #5a67d8;
      transform: translateY(-1px);
      box-shadow: 0 4px 6px rgba(102, 126, 234, 0.3);
    }

    .btn-primary:disabled {
      background: #9ca3af;
      cursor: not-allowed;
      opacity: 0.6;
    }

    /* Estilos para impresión */
    @media print {
      /* Ocultar elementos de navegación y acciones */
      app-navbar,
      .planes-header,
      .semanas-tabs,
      .btn-print-semana,
      .planes-guardados-section,
      .modal-overlay {
        display: none !important;
      }

      .plan-grid:not(.print-grid) {
        display: none !important;
      }

      .print-all-container {
        display: block !important;
      }

      /* Configuración de página */
      @page {
        margin: 1cm;
        size: letter landscape;
      }

      html, body {
        margin: 0 !important;
        padding: 0 !important;
        background: white !important;
      }

      .planes-container {
        padding: 0 !important;
        margin: 0 !important;
      }

      /* Optimizar la grilla para impresión */
      .plan-grid {
        display: grid;
        grid-template-columns: repeat(7, 1fr);
        gap: 0.5rem;
        page-break-inside: avoid;
      }

      .dia-card {
        page-break-inside: avoid;
        box-shadow: none !important;
        border: 1px solid #333;
      }

      .dia-header {
        background: #667eea !important;
        -webkit-print-color-adjust: exact !important;
        print-color-adjust: exact !important;
        padding: 0.5rem;
      }

      .dia-header h3 {
        font-size: 0.875rem;
        margin: 0;
      }

      .comidas-list {
        padding: 0.5rem;
      }

      .comida-item {
        margin-bottom: 0.5rem;
      }

      .comida-item label {
        font-size: 0.75rem;
        font-weight: 600;
      }

      .comida-item textarea {
        font-size: 0.7rem;
        padding: 0.25rem;
        border: 1px solid #999;
        min-height: 2rem;
      }
    }
  `]
})
export class PlanesAlimentacionComponent implements OnInit {
  semanaActual = signal(1);
  mensajeGuardado = signal(false);
  historiasDisponibles = signal<any[]>([]);
  historiaSeleccionada = '';
  cargando = false;
  mostrarPlanesGuardados = signal(false);
  planesGuardados = signal<any[]>([]);
  cargandoPlanes = false;
  planActualId: string | null = null;
  imprimiendoAmbasSemanas = signal(false);
  semanas = [1, 2, 3, 4];
  mostrarModalImpresion = signal(false);
  semanasParaImprimir = signal<number[]>([]);
  imprimirSemana1 = true;
  imprimirSemana2 = true;
  imprimirSemana3 = true;
  imprimirSemana4 = true;
  
  diasSemana = [
    { key: 'lunes' as const, nombre: 'Lunes' },
    { key: 'martes' as const, nombre: 'Martes' },
    { key: 'miercoles' as const, nombre: 'Miércoles' },
    { key: 'jueves' as const, nombre: 'Jueves' },
    { key: 'viernes' as const, nombre: 'Viernes' },
    { key: 'sabado' as const, nombre: 'Sábado' },
    { key: 'domingo' as const, nombre: 'Domingo' }
  ];

  semana1: SemanaPlan = this.crearSemanaVacia();
  semana2: SemanaPlan = this.crearSemanaVacia();
  semana3: SemanaPlan = this.crearSemanaVacia();
  semana4: SemanaPlan = this.crearSemanaVacia();

  constructor(private planesService: PlanesService, private http: HttpClient) {}

  ngOnInit(): void {
    this.cargarHistorias();
  }

  cargarHistorias(): void {
    this.planesService.getHistoriasDisponibles().subscribe({
      next: (historias) => {
        this.historiasDisponibles.set(historias);
      },
      error: (error) => {
        console.error('Error cargando historias:', error);
      }
    });
  }

  onHistoriaChange(): void {
    // Ocultar planes guardados al cambiar de historia
    this.mostrarPlanesGuardados.set(false);
    this.planesGuardados.set([]);
    this.planActualId = null;
    
    if (!this.historiaSeleccionada) {
      return;
    }

    this.planesService.getPlanesByHistoria(this.historiaSeleccionada).subscribe({
      next: (planes) => {
        if (planes && planes.length > 0) {
          this.cargarPlan(planes[0].id);
        } else {
          this.semana1 = this.crearSemanaVacia();
          this.semana2 = this.crearSemanaVacia();
          this.semana3 = this.crearSemanaVacia();
          this.semana4 = this.crearSemanaVacia();
        }
      },
      error: (error) => {
        console.error('Error cargando plan existente:', error);
      }
    });
  }

  getPlanActual(): SemanaPlan {
    switch(this.semanaActual()) {
      case 1: return this.semana1;
      case 2: return this.semana2;
      case 3: return this.semana3;
      case 4: return this.semana4;
      default: return this.semana1;
    }
  }

  getPlanSemana(semana: number): SemanaPlan {
    switch(semana) {
      case 1: return this.semana1;
      case 2: return this.semana2;
      case 3: return this.semana3;
      case 4: return this.semana4;
      default: return this.semana1;
    }
  }

  cambiarSemana(semana: number): void {
    this.semanaActual.set(semana);
  }

  guardarPlan(): void {
    if (!this.historiaSeleccionada) {
      alert('Por favor seleccione una historia clinica');
      return;
    }

    if (this.cargando) {
      return;
    }

    this.cargando = true;

    this.planesService.getPlanesByHistoria(this.historiaSeleccionada).subscribe({
      next: (planes) => {
        const planCompleto = {
          HistoriaId: this.historiaSeleccionada,
          FechaInicio: new Date().toISOString().split('T')[0],
          Semana1: this.semana1,
          Semana2: this.semana2,
          Semana3: this.semana3,
          Semana4: this.semana4
        };

        if (planes && planes.length > 0) {
          const planExistente = planes[0];
          this.planActualId = planExistente.id;

          this.planesService.actualizarPlan(planExistente.id, planCompleto as any).subscribe({
            next: (response) => {
              this.cargando = false;
              this.mensajeGuardado.set(true);
              setTimeout(() => this.mensajeGuardado.set(false), 3000);
            },
            error: (error) => {
              this.cargando = false;
              alert('Error al actualizar el plan. Por favor intente nuevamente.');
            }
          });
        } else {
          this.planesService.crearPlan(planCompleto as any).subscribe({
            next: (response) => {
              this.planActualId = response.planId;
              this.cargando = false;
              this.mensajeGuardado.set(true);
              setTimeout(() => this.mensajeGuardado.set(false), 3000);
            },
            error: (error) => {
              this.cargando = false;
              alert('Error al guardar el plan. Por favor intente nuevamente.');
            }
          });
        }
      },
      error: (error) => {
        this.cargando = false;
        alert('Error al verificar planes existentes.');
      }
    });
  }

  limpiarPlan(): void {
    if (!confirm('Estas seguro de que deseas limpiar toda la informacion de la semana actual?')) {
      return;
    }

    switch(this.semanaActual()) {
      case 1:
        this.semana1 = this.crearSemanaVacia();
        break;
      case 2:
        this.semana2 = this.crearSemanaVacia();
        break;
      case 3:
        this.semana3 = this.crearSemanaVacia();
        break;
      case 4:
        this.semana4 = this.crearSemanaVacia();
        break;
    }
  }

  toggleVerPlanes(): void {
    if (!this.historiaSeleccionada) {
      return;
    }
    
    const nuevoEstado = !this.mostrarPlanesGuardados();
    this.mostrarPlanesGuardados.set(nuevoEstado);
    
    if (nuevoEstado) {
      this.cargarPlanesGuardados();
    }
  }

  cargarPlanesGuardados(): void {
    if (!this.historiaSeleccionada) {
      return;
    }

    this.cargandoPlanes = true;
    
    this.planesService.getPlanesByHistoria(this.historiaSeleccionada).subscribe({
      next: (planes) => {
        this.planesGuardados.set(planes);
        this.cargandoPlanes = false;
        
        if (planes && planes.length === 1) {
          this.cargarPlan(planes[0].id);
        }
      },
      error: (error) => {
        this.cargandoPlanes = false;
        
        if (error.status === 401) {
          alert('Sesion expirada. Por favor inicie sesion nuevamente.');
        } else {
          alert('Error al cargar los planes guardados.');
        }
      }
    });
  }

  cargarPlan(planId: string): void {
    this.planesService.getPlanById(planId).subscribe({
      next: (plan) => {
        console.log('Plan cargado:', plan);
        
        this.planActualId = plan.id;
        
        if (plan.semana1) {
          this.semana1 = plan.semana1;
        }
        if (plan.semana2) {
          this.semana2 = plan.semana2;
        }
        if (plan.semana3) {
          this.semana3 = plan.semana3;
        }
        if (plan.semana4) {
          this.semana4 = plan.semana4;
        }
        
        this.mostrarPlanesGuardados.set(false);
      },
      error: (error) => {
        alert('Error al cargar el plan.');
      }
    });
  }

  eliminarPlanActual(): void {
    if (!this.planActualId) {
      alert('No hay un plan cargado para eliminar.');
      return;
    }

    this.eliminarPlan(this.planActualId);
  }

  eliminarPlan(planId: string): void {
    if (!confirm('¿Estás seguro de que deseas eliminar este plan? Esta acción no se puede deshacer.')) {
      return;
    }

    this.planesService.eliminarPlan(planId).subscribe({
      next: (response) => {
        alert('Plan eliminado exitosamente');
        
        // Si el plan eliminado es el plan actual, limpiar todo
        if (this.planActualId === planId) {
          this.planActualId = null;
          this.semana1 = this.crearSemanaVacia();
          this.semana2 = this.crearSemanaVacia();
          this.semana3 = this.crearSemanaVacia();
          this.semana4 = this.crearSemanaVacia();
        }
        
        // Recargar la lista de planes guardados
        this.cargarPlanesGuardados();
      },
      error: (error) => {
        alert('Error al eliminar el plan. Por favor intente nuevamente.');
      }
    });
  }

  tieneSemanaSeleccionada(): boolean {
    return this.imprimirSemana1 || this.imprimirSemana2 || this.imprimirSemana3 || this.imprimirSemana4;
  }

  confirmarImpresion(): void {
    const semanasSeleccionadas: number[] = [];
    if (this.imprimirSemana1) semanasSeleccionadas.push(1);
    if (this.imprimirSemana2) semanasSeleccionadas.push(2);
    if (this.imprimirSemana3) semanasSeleccionadas.push(3);
    if (this.imprimirSemana4) semanasSeleccionadas.push(4);

    this.semanasParaImprimir.set(semanasSeleccionadas);
    this.mostrarModalImpresion.set(false);
    this.imprimirSemanasSeleccionadas();
  }

  imprimirSemanasSeleccionadas(): void {
    this.imprimiendoAmbasSemanas.set(true);
    document.body.classList.add('printing-plan');

    // Dejar que Angular pinte las semanas seleccionadas y luego imprimir
    setTimeout(() => {
      window.print();

      // Restaurar modo normal después de que el navegador procese la impresión
      setTimeout(() => {
        this.imprimiendoAmbasSemanas.set(false);
        document.body.classList.remove('printing-plan');
      }, 400);
    }, 300);
  }

  imprimirTodasSemanas(): void {
    this.imprimiendoAmbasSemanas.set(true);
    document.body.classList.add('printing-plan');

    // Dejar que Angular pinte ambas semanas y luego imprimir
    setTimeout(() => {
      window.print();

      // Restaurar modo normal después de que el navegador procese la impresión
      setTimeout(() => {
        this.imprimiendoAmbasSemanas.set(false);
        document.body.classList.remove('printing-plan');
      }, 400);
    }, 300);
  }

  private crearSemanaVacia(): SemanaPlan {
    const comidaVacia = (): ComidaDia => ({
      desayuno: '',
      snack1: '',
      almuerzo: '',
      snack2: '',
      merienda: ''
    });

    return {
      lunes: comidaVacia(),
      martes: comidaVacia(),
      miercoles: comidaVacia(),
      jueves: comidaVacia(),
      viernes: comidaVacia(),
      sabado: comidaVacia(),
      domingo: comidaVacia()
    };
  }
}

