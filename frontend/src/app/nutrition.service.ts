import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NutritionService {
  private readonly baseUrl = 'http://localhost:5000/api/nutrition';

  constructor(private readonly http: HttpClient) {}

  saveRecord(payload: unknown) {
    return this.http.post(`${this.baseUrl}/history`, payload);
  }

  checkStatus() {
    return this.http.get(`${this.baseUrl}/status`);
  }

  // Obtener datos de un paciente específico
  getPaciente(id: string) {
    return this.http.get(`${this.baseUrl}/pacientes/${id}`);
  }

  // Buscar paciente por número de cédula
  buscarPacientePorCedula(cedula: string) {
    return this.http.get(`${this.baseUrl}/pacientes/buscar/cedula/${cedula}`);
  }

  // Buscar paciente por cédula con su última historia clínica
  buscarPacienteConUltimaHistoria(cedula: string) {
    return this.http.get(`${this.baseUrl}/pacientes/buscar/cedula/${cedula}/ultima-historia`);
  }

  // Obtener datos de una historia clínica específica
  getHistoriaClinica(id: string) {
    return this.http.get(`${this.baseUrl}/historias/${id}`);
  }

  // Actualizar paciente
  updatePaciente(id: string, data: any) {
    return this.http.put(`${this.baseUrl}/pacientes/${id}`, data);
  }

  // Actualizar historia clínica
  updateHistoriaClinica(id: string, data: any) {
    return this.http.put(`${this.baseUrl}/historias/${id}`, data);
  }
}
