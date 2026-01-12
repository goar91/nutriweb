import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

interface Estadisticas {
  totalPacientes: number;
  totalHistorias: number;
  pacientesMes: number;
  historiasMes: number;
  pacientesFemenino: number;
  pacientesMasculino: number;
}

interface PacienteReporte {
  id: string;
  numeroCedula: string | null;
  nombre: string;
  edadCronologica: string | null;
  sexo: string | null;
  telefono: string | null;
  email: string | null;
  fechaCreacion: string;
  totalHistorias: number;
  ultimaConsulta: string | null;
}

interface HistoriaReporte {
  historiaId: string;
  fechaConsulta: string | null;
  motivoConsulta: string | null;
  diagnostico: string | null;
  fechaRegistro: string;
  pacienteId: string;
  numeroCedula: string | null;
  nombrePaciente: string;
  edad: string | null;
  sexo: string | null;
  imc: number | null;
  peso: number | null;
  talla: number | null;
}

@Component({
  selector: 'app-reportes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reportes.component.html',
  styleUrls: ['./reportes.component.scss']
})
export class ReportesComponent implements OnInit {
  private apiUrl = 'http://localhost:5000/api/reportes';
  
  estadisticas = signal<Estadisticas | null>(null);
  pacientes = signal<PacienteReporte[]>([]);
  historias = signal<HistoriaReporte[]>([]);
  
  loading = signal(false);
  error = signal<string | null>(null);
  
  tipoReporte = signal<'estadisticas' | 'pacientes' | 'historias'>('estadisticas');
  fechaDesde = signal('');
  fechaHasta = signal('');

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadEstadisticas();
  }

  loadEstadisticas(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<Estadisticas>(`${this.apiUrl}/estadisticas`).subscribe({
      next: (data) => {
        this.estadisticas.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error cargando estadísticas:', error);
        this.error.set('Error al cargar las estadísticas');
        this.loading.set(false);
      }
    });
  }

  loadReportePacientes(): void {
    this.loading.set(true);
    this.error.set(null);

    const params: any = {};
    if (this.fechaDesde()) params.fechaDesde = this.fechaDesde();
    if (this.fechaHasta()) params.fechaHasta = this.fechaHasta();

    const queryString = new URLSearchParams(params).toString();
    const url = `${this.apiUrl}/pacientes${queryString ? '?' + queryString : ''}`;

    this.http.get<PacienteReporte[]>(url).subscribe({
      next: (data) => {
        this.pacientes.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error cargando reporte de pacientes:', error);
        this.error.set('Error al cargar el reporte de pacientes');
        this.loading.set(false);
      }
    });
  }

  loadReporteHistorias(): void {
    this.loading.set(true);
    this.error.set(null);

    const params: any = {};
    if (this.fechaDesde()) params.fechaDesde = this.fechaDesde();
    if (this.fechaHasta()) params.fechaHasta = this.fechaHasta();

    const queryString = new URLSearchParams(params).toString();
    const url = `${this.apiUrl}/historias${queryString ? '?' + queryString : ''}`;

    this.http.get<HistoriaReporte[]>(url).subscribe({
      next: (data) => {
        this.historias.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error cargando reporte de historias:', error);
        this.error.set('Error al cargar el reporte de historias');
        this.loading.set(false);
      }
    });
  }

  cambiarTipoReporte(tipo: 'estadisticas' | 'pacientes' | 'historias'): void {
    this.tipoReporte.set(tipo);
    this.error.set(null);

    if (tipo === 'estadisticas') {
      this.loadEstadisticas();
    } else if (tipo === 'pacientes') {
      this.loadReportePacientes();
    } else if (tipo === 'historias') {
      this.loadReporteHistorias();
    }
  }

  generarReporte(): void {
    if (this.tipoReporte() === 'pacientes') {
      this.loadReportePacientes();
    } else if (this.tipoReporte() === 'historias') {
      this.loadReporteHistorias();
    }
  }

  exportarCSV(): void {
    let csvContent = '';
    let filename = '';

    if (this.tipoReporte() === 'pacientes') {
      csvContent = this.generarCSVPacientes();
      filename = 'reporte_pacientes.csv';
    } else if (this.tipoReporte() === 'historias') {
      csvContent = this.generarCSVHistorias();
      filename = 'reporte_historias.csv';
    }

    if (csvContent) {
      const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
      const link = document.createElement('a');
      const url = URL.createObjectURL(blob);
      link.setAttribute('href', url);
      link.setAttribute('download', filename);
      link.style.visibility = 'hidden';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
  }

  private generarCSVPacientes(): string {
    const headers = ['Cédula', 'Nombre', 'Edad', 'Sexo', 'Teléfono', 'Email', 'Total Historias', 'Última Consulta', 'Fecha Registro'];
    const rows = this.pacientes().map(p => [
      p.numeroCedula || '',
      p.nombre,
      p.edadCronologica || '',
      p.sexo || '',
      p.telefono || '',
      p.email || '',
      p.totalHistorias.toString(),
      p.ultimaConsulta ? this.formatDate(p.ultimaConsulta) : '',
      this.formatDate(p.fechaCreacion)
    ]);

    return [headers, ...rows].map(row => row.map(cell => `"${cell}"`).join(',')).join('\n');
  }

  private generarCSVHistorias(): string {
    const headers = ['Fecha Consulta', 'Paciente', 'Cédula', 'Edad', 'Sexo', 'Motivo', 'Diagnóstico', 'IMC', 'Peso', 'Talla'];
    const rows = this.historias().map(h => [
      h.fechaConsulta ? this.formatDate(h.fechaConsulta) : '',
      h.nombrePaciente,
      h.numeroCedula || '',
      h.edad || '',
      h.sexo || '',
      h.motivoConsulta || '',
      h.diagnostico || '',
      h.imc?.toString() || '',
      h.peso?.toString() || '',
      h.talla?.toString() || ''
    ]);

    return [headers, ...rows].map(row => row.map(cell => `"${cell}"`).join(',')).join('\n');
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('es-ES');
  }
}
