import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ComidaDia {
  desayuno: string;
  snack1: string;
  almuerzo: string;
  snack2: string;
  merienda: string;
}

export interface SemanaPlan {
  lunes: ComidaDia;
  martes: ComidaDia;
  miercoles: ComidaDia;
  jueves: ComidaDia;
  viernes: ComidaDia;
  sabado: ComidaDia;
  domingo: ComidaDia;
}

export interface PlanAlimentacion {
  id?: string;
  historia_id: string;
  fecha_inicio: string;
  fecha_fin?: string;
  objetivo?: string;
  observaciones?: string;
  semana1: SemanaPlan;
  semana2: SemanaPlan;
  semana3?: SemanaPlan;
  semana4?: SemanaPlan;
}

export interface PlanNutricional {
  id?: string;
  historia_id: string;
  fecha_inicio: string;
  fecha_fin?: string;
  objetivo?: string;
  calorias_diarias?: number;
  observaciones?: string;
  activo?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PlanesService {
  private apiUrl = 'http://localhost:5000/api/planes';

  constructor(private http: HttpClient) {}

  // Obtener todos los planes de una historia clínica
  getPlanesByHistoria(historiaId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/historia/${historiaId}`);
  }

  // Obtener un plan específico con su alimentación semanal
  getPlanById(planId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${planId}`);
  }

  // Crear un nuevo plan con alimentación
  crearPlan(plan: PlanAlimentacion): Observable<any> {
    return this.http.post<any>(this.apiUrl, plan);
  }

  // Actualizar un plan existente
  actualizarPlan(planId: string, plan: PlanAlimentacion): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${planId}`, plan);
  }

  // Eliminar un plan
  eliminarPlan(planId: string): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${planId}`);
  }

  // Listar todas las historias clínicas disponibles (para selector)
  getHistoriasDisponibles(): Observable<any[]> {
    return this.http.get<any[]>('http://localhost:5000/api/historias/list');
  }
}
