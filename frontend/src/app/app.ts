import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { NutritionService } from './nutrition.service';

interface FoodCategory {
  title: string;
  items: string[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly pageTitle = signal('NutriWeb · Historia clínica nutricional');
  protected readonly supportedFrequencies = [
    'Nunca',
    '1–3/mes',
    '1/sem',
    '2–4/sem',
    '5–6/sem',
    '1/día',
    '≥2/día'
  ];

  protected readonly foodCategories: FoodCategory[] = [
    {
      title: '1. Cereales y tubérculos',
      items: ['Arroz', 'Pan', 'Avena', 'Yuca', 'Papa', 'Fideos / pasta', 'Quinua']
    },
    {
      title: '2. Verduras',
      items: ['Hojas verdes', 'Brócoli', 'Tomate', 'Zanahoria', 'Pepino']
    },
    {
      title: '3. Frutas',
      items: ['Manzana', 'Banano', 'Cítricos', 'Fresas', 'Papaya']
    },
    {
      title: '4. Proteínas animales',
      items: ['Pollo', 'Carne de res', 'Pescado', 'Huevo', 'Cerdo']
    },
    {
      title: '5. Proteínas vegetales',
      items: ['Legumbres', 'Tofu / soya', 'Frutos secos']
    },
    {
      title: '6. Lácteos',
      items: ['Leche', 'Yogur', 'Queso']
    },
    {
      title: '7. Grasas',
      items: ['Aguacate', 'Aceite vegetal', 'Mantequilla']
    },
    {
      title: '8. Bebidas',
      items: ['Agua', 'Jugos azucarados', 'Gaseosas', 'Café', 'Té / infusiones']
    },
    {
      title: '9. Ultraprocesados',
      items: ['Snacks', 'Galletas', 'Pasteles / dulces', 'Comida rápida']
    }
  ];

  protected readonly nutriForm: FormGroup;

  protected readonly resultMessage = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected frequencySelections: Record<string, string> = {};

  private buildForm(): FormGroup {
    return this.fb.group({
      personalData: this.fb.group({
        nombre: [''],
        edadCronologica: [''],
        sexo: [''],
        lugarResidencia: [''],
        estadoCivil: [''],
        numeroCedula: [''],
        telefono: [''],
        ocupacion: [''],
        email: [''],
        fechaConsulta: ['']
      }),
      motivoConsulta: [''],
      antecedentes: this.fb.group({
        apf: [''],
        app: [''],
        apq: [''],
        ago: [''],
        menarquia: [''],
        p: [''],
        g: [''],
        c: [''],
        a: [''],
        alergias: ['']
      }),
      diagnostico: [''],
      habitos: this.fb.group({
        fuma: [''],
        alcohol: [''],
        cafe: [''],
        hidratacion: [''],
        gaseosas: [''],
        actividadFisica: [''],
        te: [''],
        edulcorantes: [''],
        alimentacion: ['']
      }),
      signos: this.fb.group({
        pa: [''],
        temperatura: [''],
        fc: [''],
        fr: ['']
      }),
      antropometricos: this.fb.group({
        edad: [''],
        edadMetabolica: [''],
        sexo: [''],
        peso: [''],
        masaMuscular: [''],
        gcPorc: [''],
        gc: [''],
        talla: [''],
        gvPorc: [''],
        imc: [''],
        kcalBasales: [''],
        actividadFisica: [''],
        cintura: [''],
        cadera: [''],
        pantorrilla: [''],
        cBrazo: [''],
        cMuslo: [''],
        pesoAjustado: [''],
        factorActividadFisica: [''],
        tiemposComida: ['']
      }),
      valoresBioquimicos: this.fb.group({
        glicemia: [''],
        colesterolTotal: [''],
        trigliceridos: [''],
        hdl: [''],
        ldl: [''],
        tgo: [''],
        tgp: [''],
        urea: [''],
        creatinina: ['']
      }),
      recordatorio24h: this.fb.group({
        desayuno: [''],
        snack1: [''],
        almuerzo: [''],
        snack2: [''],
        cena: [''],
        extras: ['']
      }),
      notasExtras: ['']
    });
  }

  constructor(
    private readonly fb: FormBuilder,
    private readonly nutritionService: NutritionService
  ) {
    this.nutriForm = this.buildForm();
  }

  protected submitHistory(): void {
    if (this.nutriForm.invalid) {
      this.nutriForm.markAllAsTouched();
    }

    this.isSubmitting.set(true);
    const payload = {
      ...this.nutriForm.value,
      frequency: this.frequencySelections,
      submittedAt: new Date().toISOString()
    };

    this.nutritionService.saveRecord(payload).subscribe({
      next: () => {
        this.resultMessage.set('Historia guardada en el servidor.');
        this.isSubmitting.set(false);
      },
      error: (error) => {
        console.error(error);
        this.resultMessage.set(
          'No se pudo guardar la historia, revise la conexión con el backend.'
        );
        this.isSubmitting.set(false);
      }
    });
  }

  protected resetForm(): void {
    this.nutriForm.reset();
    this.frequencySelections = {};
    this.resultMessage.set(null);
  }

  protected getFrequency(category: string, food: string): string {
    return (
      this.frequencySelections[this.frequencyKey(category, food)] ??
      this.supportedFrequencies[0]
    );
  }

  protected setFrequency(category: string, food: string, value: string): void {
    const key = this.frequencyKey(category, food);
    this.frequencySelections = {
      ...this.frequencySelections,
      [key]: value
    };
  }

  protected frequencyKey(category: string, food: string): string {
    return `${category}::${food}`;
  }
}
