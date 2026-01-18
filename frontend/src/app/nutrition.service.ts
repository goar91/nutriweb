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

  // Obtener datos de un paciente especÃ­fico
  getPaciente(id: string) {
    return this.http.get(`${this.baseUrl}/pacientes/${id}`);
  }

  // Buscar paciente por nÃºmero de cÃ©dula
  buscarPacientePorCedula(cedula: string) {
    return this.http.get(`${this.baseUrl}/pacientes/buscar/cedula/${cedula}`);
  }

  // Buscar paciente por cÃ©dula con su Ãºltima historia clÃ­nica
  buscarPacienteConUltimaHistoria(cedula: string) {
    return this.http.get(`${this.baseUrl}/pacientes/buscar/cedula/${cedula}/ultima-historia`);
  }

  // Obtener datos de una historia clÃ­nica especÃ­fica
  getHistoriaClinica(id: string) {
    return this.http.get(`${this.baseUrl}/historias/${id}`);
  }

  // Actualizar paciente
  updatePaciente(id: string, data: any) {
    return this.http.put(`${this.baseUrl}/pacientes/${id}`, data);
  }

  // Eliminar paciente
  deletePaciente(id: string) {
    return this.http.delete(`${this.baseUrl}/pacientes/${id}`);
  }

  // Actualizar historia clÃ­nica
  updateHistoriaClinica(id: string, data: any) {
    return this.http.put(`${this.baseUrl}/historias/${id}`, data);
  }

  // Eliminar historia clínica
  deleteHistoriaClinica(id: string) {
    return this.http.delete(`${this.baseUrl}/historias/${id}`);
  }
}
