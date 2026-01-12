import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface Paciente {
  id: string;
  numeroCedula: string | null;
  nombre: string;
  edadCronologica: string | null;
  sexo: string | null;
  telefono: string | null;
  email: string | null;
  fechaCreacion: string;
  fechaActualizacion: string;
}

@Component({
  selector: 'app-pacientes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pacientes.component.html',
  styleUrls: ['./pacientes.component.scss']
})
export class PacientesComponent implements OnInit {
  private apiUrl = 'http://localhost:5000/api';
  
  pacientes = signal<Paciente[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  searchTerm = signal('');
  
  constructor(
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadPacientes();
  }

  loadPacientes(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<Paciente[]>(`${this.apiUrl}/pacientes`).subscribe({
      next: (data) => {
        this.pacientes.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error cargando pacientes:', error);
        this.error.set('Error al cargar los pacientes');
        this.loading.set(false);
      }
    });
  }

  get filteredPacientes(): Paciente[] {
    const search = this.searchTerm().toLowerCase();
    if (!search) return this.pacientes();

    return this.pacientes().filter(p => 
      p.nombre?.toLowerCase().includes(search) ||
      p.numeroCedula?.toLowerCase().includes(search) ||
      p.email?.toLowerCase().includes(search) ||
      p.telefono?.includes(search)
    );
  }

  verDetalles(pacienteId: string): void {
    this.router.navigate(['/pacientes', pacienteId]);
  }

  eliminarPaciente(paciente: Paciente): void {
    if (!confirm(`¿Está seguro de eliminar al paciente ${paciente.nombre}?`)) {
      return;
    }

    this.http.delete(`${this.apiUrl}/pacientes/${paciente.id}`).subscribe({
      next: () => {
        this.loadPacientes();
      },
      error: (error) => {
        console.error('Error eliminando paciente:', error);
        alert('Error al eliminar el paciente');
      }
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('es-ES');
  }
}
