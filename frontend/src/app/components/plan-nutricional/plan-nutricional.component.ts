import { Component, signal, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';

interface AlimentacionDia {
  semana: number;
  dia_semana: number;
  desayuno: string;
  snack_manana: string;
  almuerzo: string;
  snack_tarde: string;
  cena: string;
  snack_noche: string;
  observaciones: string;
}

interface Plan {
  historiaId: string;
  fechaInicio: string;
  fechaFin: string;
  objetivo: string;
  caloriasDiarias: number | null;
  observaciones: string;
  activo: boolean;
  alimentacionSemanal: AlimentacionDia[];
}

@Component({
  selector: 'app-plan-nutricional',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="plan-container">
      <h2>Plan Nutricional</h2>
      
      <div class="plan-form">
        <div class="form-section">
          <h3>Información del Plan</h3>
          
          <div class="form-row">
            <div class="form-group">
              <label>Fecha Inicio *</label>
              <input type="date" [(ngModel)]="plan().fechaInicio" required>
            </div>
            
            <div class="form-group">
              <label>Fecha Fin</label>
              <input type="date" [(ngModel)]="plan().fechaFin">
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Calorías Diarias</label>
              <input type="number" [(ngModel)]="plan().caloriasDiarias" step="0.01">
            </div>
            
            <div class="form-group">
              <label>Estado</label>
              <select [(ngModel)]="plan().activo">
                <option [value]="true">Activo</option>
                <option [value]="false">Inactivo</option>
              </select>
            </div>
          </div>

          <div class="form-group full-width">
            <label>Objetivo</label>
            <textarea [(ngModel)]="plan().objetivo" rows="3"></textarea>
          </div>

          <div class="form-group full-width">
            <label>Observaciones</label>
            <textarea [(ngModel)]="plan().observaciones" rows="3"></textarea>
          </div>
        </div>

        <div class="form-section">
          <h3>Alimentación - 2 Semanas</h3>
          
          <div class="tabs">
            <button 
              *ngFor="let sem of [1, 2]" 
              (click)="semanaActiva.set(sem)"
              [class.active]="semanaActiva() === sem">
              Semana {{ sem }}
            </button>
          </div>

          <div class="dias-container">
            <div *ngFor="let dia of diasSemana; let i = index" class="dia-card">
              <h4>{{ dia }}</h4>
              
              <div class="comida-group">
                <label>Desayuno</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).desayuno" rows="2"></textarea>
              </div>

              <div class="comida-group">
                <label>Snack Mañana</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).snack_manana" rows="2"></textarea>
              </div>

              <div class="comida-group">
                <label>Almuerzo</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).almuerzo" rows="2"></textarea>
              </div>

              <div class="comida-group">
                <label>Snack Tarde</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).snack_tarde" rows="2"></textarea>
              </div>

              <div class="comida-group">
                <label>Cena</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).cena" rows="2"></textarea>
              </div>

              <div class="comida-group">
                <label>Snack Noche</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).snack_noche" rows="2"></textarea>
              </div>

              <div class="comida-group">
                <label>Observaciones</label>
                <textarea [(ngModel)]="getAlimentacionDia(semanaActiva(), i + 1).observaciones" rows="1"></textarea>
              </div>
            </div>
          </div>
        </div>

        <div class="form-actions">
          <button (click)="guardarPlan()" class="btn-primary">Guardar Plan</button>
          <button (click)="cancelar()" class="btn-secondary">Cancelar</button>
        </div>

        <div *ngIf="mensaje()" class="mensaje" [class.error]="esError()">
          {{ mensaje() }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .plan-container {
      padding: 20px;
      max-width: 1400px;
      margin: 0 auto;
    }

    h2 {
      color: #2c3e50;
      margin-bottom: 30px;
    }

    .plan-form {
      background: white;
      padding: 30px;
      border-radius: 10px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    }

    .form-section {
      margin-bottom: 40px;
    }

    .form-section h3 {
      color: #34495e;
      margin-bottom: 20px;
      padding-bottom: 10px;
      border-bottom: 2px solid #3498db;
    }

    .form-row {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 20px;
      margin-bottom: 20px;
    }

    .form-group {
      display: flex;
      flex-direction: column;
    }

    .form-group.full-width {
      margin-bottom: 20px;
    }

    label {
      font-weight: 600;
      color: #555;
      margin-bottom: 5px;
    }

    input, select, textarea {
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 5px;
      font-size: 14px;
      font-family: inherit;
    }

    input:focus, select:focus, textarea:focus {
      outline: none;
      border-color: #3498db;
      box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.1);
    }

    .tabs {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
    }

    .tabs button {
      padding: 10px 20px;
      border: 2px solid #3498db;
      background: white;
      color: #3498db;
      border-radius: 5px;
      cursor: pointer;
      font-weight: 600;
      transition: all 0.3s;
    }

    .tabs button.active {
      background: #3498db;
      color: white;
    }

    .tabs button:hover:not(.active) {
      background: #ebf5fb;
    }

    .dias-container {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 20px;
    }

    .dia-card {
      background: #f8f9fa;
      padding: 20px;
      border-radius: 8px;
      border-left: 4px solid #3498db;
    }

    .dia-card h4 {
      color: #2c3e50;
      margin-bottom: 15px;
    }

    .comida-group {
      margin-bottom: 15px;
    }

    .comida-group label {
      font-size: 13px;
      color: #666;
      margin-bottom: 3px;
    }

    .comida-group textarea {
      font-size: 13px;
      resize: vertical;
    }

    .form-actions {
      display: flex;
      gap: 15px;
      justify-content: flex-end;
      margin-top: 30px;
      padding-top: 20px;
      border-top: 1px solid #ddd;
    }

    .btn-primary, .btn-secondary {
      padding: 12px 30px;
      border: none;
      border-radius: 5px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.3s;
    }

    .btn-primary {
      background: #27ae60;
      color: white;
    }

    .btn-primary:hover {
      background: #229954;
      transform: translateY(-2px);
      box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    }

    .btn-secondary {
      background: #95a5a6;
      color: white;
    }

    .btn-secondary:hover {
      background: #7f8c8d;
    }

    .mensaje {
      margin-top: 20px;
      padding: 15px;
      border-radius: 5px;
      background: #d4edda;
      color: #155724;
      border: 1px solid #c3e6cb;
    }

    .mensaje.error {
      background: #f8d7da;
      color: #721c24;
      border-color: #f5c6cb;
    }
  `]
})
export class PlanNutricionalComponent {
  historiaId = input.required<string>();
  
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/nutrition';

  semanaActiva = signal(1);
  mensaje = signal('');
  esError = signal(false);

  diasSemana = ['Lunes', 'Martes', 'Miércoles', 'Jueves', 'Viernes', 'Sábado', 'Domingo'];

  plan = signal<Plan>({
    historiaId: '',
    fechaInicio: this.getFechaHoy(),
    fechaFin: '',
    objetivo: '',
    caloriasDiarias: null,
    observaciones: '',
    activo: true,
    alimentacionSemanal: this.inicializarAlimentacion()
  });

  constructor() {
    // Actualizar historiaId cuando cambie el input
    this.plan.update(p => ({ ...p, historiaId: this.historiaId() }));
  }

  getFechaHoy(): string {
    return new Date().toISOString().split('T')[0];
  }

  inicializarAlimentacion(): AlimentacionDia[] {
    const alimentacion: AlimentacionDia[] = [];
    for (let semana = 1; semana <= 2; semana++) {
      for (let dia = 1; dia <= 7; dia++) {
        alimentacion.push({
          semana,
          dia_semana: dia,
          desayuno: '',
          snack_manana: '',
          almuerzo: '',
          snack_tarde: '',
          cena: '',
          snack_noche: '',
          observaciones: ''
        });
      }
    }
    return alimentacion;
  }

  getAlimentacionDia(semana: number, dia: number): AlimentacionDia {
    const found = this.plan().alimentacionSemanal.find(
      a => a.semana === semana && a.dia_semana === dia
    );
    return found || {
      semana,
      dia_semana: dia,
      desayuno: '',
      snack_manana: '',
      almuerzo: '',
      snack_tarde: '',
      cena: '',
      snack_noche: '',
      observaciones: ''
    };
  }

  guardarPlan() {
    const token = sessionStorage.getItem('authToken');
    if (!token) {
      this.mostrarMensaje('No hay sesión activa', true);
      return;
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });

    const planData = {
      historiaId: this.historiaId(),
      fechaInicio: this.plan().fechaInicio,
      fechaFin: this.plan().fechaFin || null,
      objetivo: this.plan().objetivo,
      caloriasDiarias: this.plan().caloriasDiarias,
      observaciones: this.plan().observaciones,
      activo: this.plan().activo,
      alimentacionSemanal: this.plan().alimentacionSemanal.filter(a => 
        a.desayuno || a.snack_manana || a.almuerzo || a.snack_tarde || a.cena || a.snack_noche
      )
    };

    this.http.post(`${this.apiUrl}/planes`, planData, { headers }).subscribe({
      next: (response: any) => {
        this.mostrarMensaje('Plan guardado exitosamente', false);
        setTimeout(() => {
          this.limpiarFormulario();
        }, 2000);
      },
      error: (error) => {
        this.mostrarMensaje('Error al guardar el plan', true);
      }
    });
  }

  mostrarMensaje(texto: string, error: boolean) {
    this.mensaje.set(texto);
    this.esError.set(error);
    setTimeout(() => {
      this.mensaje.set('');
    }, 5000);
  }

  limpiarFormulario() {
    this.plan.set({
      historiaId: this.historiaId(),
      fechaInicio: this.getFechaHoy(),
      fechaFin: '',
      objetivo: '',
      caloriasDiarias: null,
      observaciones: '',
      activo: true,
      alimentacionSemanal: this.inicializarAlimentacion()
    });
    this.semanaActiva.set(1);
  }

  cancelar() {
    this.limpiarFormulario();
  }
}
