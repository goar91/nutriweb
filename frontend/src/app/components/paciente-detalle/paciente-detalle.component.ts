import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';

interface Paciente {
  id: string;
  numeroCedula: string | null;
  nombre: string;
  edadCronologica: string | null;
  sexo: string | null;
  lugarResidencia: string | null;
  estadoCivil: string | null;
  telefono: string | null;
  ocupacion: string | null;
  email: string | null;
  fechaCreacion: string;
  fechaActualizacion: string;
}

interface Historia {
  id: string;
  fechaConsulta: string | null;
  motivoConsulta: string | null;
  diagnostico: string | null;
  fechaRegistro: string;
  imc: number | null;
  peso: number | null;
  talla: number | null;
}

@Component({
  selector: 'app-paciente-detalle',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './paciente-detalle.component.html',
  styleUrls: ['./paciente-detalle.component.scss']
})
export class PacienteDetalleComponent implements OnInit {
  private apiUrl = 'http://localhost:5000/api';
  
  paciente = signal<Paciente | null>(null);
  historias = signal<Historia[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  pacienteId: string | null = null;

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.pacienteId = this.route.snapshot.paramMap.get('id');
    if (this.pacienteId) {
      this.loadPaciente();
      this.loadHistorias();
    }
  }

  loadPaciente(): void {
    if (!this.pacienteId) return;

    this.loading.set(true);
    this.error.set(null);

    this.http.get<Paciente>(`${this.apiUrl}/pacientes/${this.pacienteId}`).subscribe({
      next: (data) => {
        this.paciente.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error cargando paciente:', error);
        this.error.set('Error al cargar los datos del paciente');
        this.loading.set(false);
      }
    });
  }

  loadHistorias(): void {
    if (!this.pacienteId) return;

    this.http.get<Historia[]>(`${this.apiUrl}/pacientes/${this.pacienteId}/historias`).subscribe({
      next: (data) => {
        this.historias.set(data);
      },
      error: (error) => {
        console.error('Error cargando historias:', error);
      }
    });
  }

  volver(): void {
    this.router.navigate(['/pacientes']);
  }

  formatDate(dateString: string | null): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('es-ES');
  }
}
