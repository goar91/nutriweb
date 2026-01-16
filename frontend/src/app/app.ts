import { CommonModule } from '@angular/common';
import { Component, signal, OnInit, inject, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NutritionService } from './nutrition.service';
import { EditDataService } from './services/edit-data.service';
import { debounceTime, distinctUntilChanged, Subscription } from 'rxjs';

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
export class App implements OnInit, OnDestroy {
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
  protected readonly isEditingPatient = signal(true);
  protected readonly isEditMode = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly editType = signal<'paciente' | 'historia' | null>(null);

  protected frequencySelections: Record<string, string> = {};
  private cedulaSubscription?: Subscription;

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
    private readonly nutritionService: NutritionService,
    private readonly editDataService: EditDataService,
    private readonly router: Router
  ) {
    this.nutriForm = this.buildForm();
  }

  ngOnInit(): void {
    console.log('====== APP COMPONENT ngOnInit ======');
    // Verificar si hay datos para editar
    const editContext = this.editDataService.getEditContext();
    console.log('Edit context on init:', editContext);
    if (editContext) {
      this.loadDataForEdit(editContext);
    } else {
      console.log('No edit context found');
    }

    // Configurar autocompletado por cédula
    this.setupCedulaAutocomplete();
  }

  ngOnDestroy(): void {
    // Limpiar suscripción
    if (this.cedulaSubscription) {
      this.cedulaSubscription.unsubscribe();
    }
  }

  private setupCedulaAutocomplete(): void {
    const cedulaControl = this.nutriForm.get('personalData.numeroCedula');
    if (cedulaControl) {
      this.cedulaSubscription = cedulaControl.valueChanges
        .pipe(
          debounceTime(500),
          distinctUntilChanged()
        )
        .subscribe((cedula: string) => {
          // Solo buscar si hay una cédula, no estamos en modo edición, y tiene al menos 6 caracteres
          if (cedula && cedula.trim().length >= 6 && !this.isEditMode()) {
            this.buscarYAutocompletarPorCedula(cedula.trim());
          }
        });
    }
  }

  private buscarYAutocompletarPorCedula(cedula: string): void {
    this.nutritionService.buscarPacienteConUltimaHistoria(cedula).subscribe({
      next: (data: any) => {
        // Si encontramos un paciente, autocompletar los campos
        console.log('Paciente encontrado con cédula:', cedula, data);
        
        // Cargar datos personales
        const personalDataGroup = this.nutriForm.get('personalData');
        if (personalDataGroup && data.paciente) {
          personalDataGroup.patchValue({
            nombre: data.paciente.nombre || '',
            edadCronologica: data.paciente.edad_cronologica || '',
            sexo: data.paciente.sexo || '',
            lugarResidencia: data.paciente.lugar_residencia || '',
            estadoCivil: data.paciente.estado_civil || '',
            telefono: data.paciente.telefono || '',
            ocupacion: data.paciente.ocupacion || '',
            email: data.paciente.email || ''
            // No modificamos numeroCedula ni fechaConsulta
          });
        }

        // Si hay última historia, cargar antecedentes, hábitos, valores bioquímicos y recordatorio 24h
        if (data.ultima_historia) {
          // Cargar antecedentes
          if (data.ultima_historia.antecedentes) {
            const ant = data.ultima_historia.antecedentes;
            this.nutriForm.patchValue({
              antecedentes: {
                apf: ant.apf || '',
                app: ant.app || '',
                apq: ant.apq || '',
                ago: ant.ago || '',
                menarquia: ant.menarquia || '',
                p: ant.p || '',
                g: ant.g || '',
                c: ant.c || '',
                a: ant.a || '',
                alergias: ant.alergias || ''
              }
            });
          }

          // Cargar hábitos
          if (data.ultima_historia.habitos) {
            const hab = data.ultima_historia.habitos;
            this.nutriForm.patchValue({
              habitos: {
                fuma: hab.fuma || '',
                alcohol: hab.alcohol || '',
                cafe: hab.cafe || '',
                hidratacion: hab.hidratacion || '',
                gaseosas: hab.gaseosas || '',
                actividadFisica: hab.actividad_fisica || '',
                te: hab.te || '',
                edulcorantes: hab.edulcorantes || '',
                alimentacion: hab.alimentacion || ''
              }
            });
          }

          // Cargar valores bioquímicos
          if (data.ultima_historia.valores_bioquimicos) {
            const bio = data.ultima_historia.valores_bioquimicos;
            this.nutriForm.patchValue({
              valoresBioquimicos: {
                glicemia: bio.glicemia || '',
                colesterolTotal: bio.colesterol_total || '',
                trigliceridos: bio.trigliceridos || '',
                hdl: bio.hdl || '',
                ldl: bio.ldl || '',
                tgo: bio.tgo || '',
                tgp: bio.tgp || '',
                urea: bio.urea || '',
                creatinina: bio.creatinina || ''
              }
            });
          }

          // Cargar recordatorio 24h
          if (data.ultima_historia.recordatorio_24h) {
            const rec = data.ultima_historia.recordatorio_24h;
            this.nutriForm.patchValue({
              recordatorio24h: {
                desayuno: rec.desayuno || '',
                snack1: rec.snack1 || '',
                almuerzo: rec.almuerzo || '',
                snack2: rec.snack2 || '',
                cena: rec.cena || '',
                extras: rec.extras || ''
              }
            });
          }

          // Mostrar mensaje informativo con más detalles
          this.resultMessage.set('✓ Datos del paciente y su última historia clínica cargados automáticamente');
          setTimeout(() => this.resultMessage.set(null), 4000);
        } else {
          // Solo datos del paciente
          this.resultMessage.set('✓ Datos del paciente cargados automáticamente');
          setTimeout(() => this.resultMessage.set(null), 3000);
        }
      },
      error: (error) => {
        // Si no se encuentra el paciente (404), no hacer nada - es un nuevo paciente
        if (error.status !== 404) {
          console.error('Error buscando paciente por cédula:', error);
        }
      }
    });
  }

  private loadDataForEdit(editContext: any): void {
    console.log('loadDataForEdit - editContext:', editContext);
    console.log('editContext.type:', editContext.type);
    console.log('editContext.data:', editContext.data);
    
    if (editContext.type === 'historia' && editContext.data) {
      this.isEditMode.set(true);
      this.editingId.set(editContext.id);
      this.editType.set('historia');
      this.pageTitle.set('Editar Historia Clínica');
      console.log('Cargando historia clínica - editType set to:', this.editType());
      this.loadHistoriaData(editContext.data);
    } else if (editContext.type === 'paciente') {
      this.isEditMode.set(true);
      this.editingId.set(editContext.id);
      this.editType.set('paciente');
      this.pageTitle.set('Editar Datos del Paciente');
      console.log('Cargando paciente - editType set to:', this.editType());
      // Cargar datos del paciente
      this.nutritionService.getPaciente(editContext.id).subscribe({
        next: (data: any) => {
          this.loadPacienteData(data);
        },
        error: (error) => {
          console.error('Error cargando paciente:', error);
          this.resultMessage.set('Error al cargar los datos del paciente');
        }
      });
    } else {
      console.warn('No se pudo determinar el tipo de edición o faltan datos');
    }
    // Limpiar el contexto de edición después de cargarlo
    this.editDataService.clearEditContext();
  }

  private loadHistoriaData(data: any): void {
    console.log('Cargando historia con datos:', data);
    
    // Cargar datos personales
    if (data.personal_data) {
      this.nutriForm.patchValue({
        personalData: {
          nombre: data.personal_data.nombre || '',
          edadCronologica: data.personal_data.edad_cronologica || '',
          sexo: data.personal_data.sexo || '',
          lugarResidencia: data.personal_data.lugar_residencia || '',
          estadoCivil: data.personal_data.estado_civil || '',
          numeroCedula: data.personal_data.numero_cedula || '',
          telefono: data.personal_data.telefono || '',
          ocupacion: data.personal_data.ocupacion || '',
          email: data.personal_data.email || '',
          fechaConsulta: data.fecha_consulta || ''
        }
      });
    }

    // Cargar motivo de consulta y diagnóstico
    this.nutriForm.patchValue({
      motivoConsulta: data.motivo_consulta || '',
      diagnostico: data.diagnostico || ''
    });

    // Cargar datos antropométricos
    if (data.datos_antropometricos) {
      const antro = data.datos_antropometricos;
      this.nutriForm.patchValue({
        antropometricos: {
          peso: antro.peso || '',
          talla: antro.talla || '',
          imc: antro.imc || '',
          cintura: antro.circunferencia_cintura || '',
          cadera: antro.circunferencia_cadera || '',
          cBrazo: antro.circunferencia_brazo || '',
          cMuslo: antro.circunferencia_muslo || '',
          pantorrilla: antro.circunferencia_pantorrilla || '',
          masaMuscular: antro.masa_muscular || '',
          gcPorc: antro.grasa_corporal_porcentaje || '',
          gc: antro.grasa_corporal || '',
          gvPorc: antro.grasa_visceral_porcentaje || '',
          edad: antro.edad || '',
          sexo: antro.sexo || '',
          edadMetabolica: antro.edad_metabolica || '',
          kcalBasales: antro.kcal_basales || '',
          actividadFisica: antro.actividad_fisica || '',
          pesoAjustado: antro.peso_ajustado || '',
          factorActividadFisica: antro.factor_actividad_fisica || '',
          tiemposComida: antro.tiempos_comida || ''
        }
      });
    }

    // Cargar signos vitales
    if (data.signos_vitales) {
      const signos = data.signos_vitales;
      this.nutriForm.patchValue({
        signos: {
          pa: signos.presion_arterial || '',
          fc: signos.frecuencia_cardiaca || '',
          fr: signos.frecuencia_respiratoria || '',
          temperatura: signos.temperatura || ''
        }
      });
    }

    // Cargar antecedentes
    if (data.antecedentes) {
      const ant = data.antecedentes;
      this.nutriForm.patchValue({
        antecedentes: {
          apf: ant.apf || '',
          app: ant.app || '',
          apq: ant.apq || '',
          ago: ant.ago || '',
          menarquia: ant.menarquia || '',
          p: ant.p || '',
          g: ant.g || '',
          c: ant.c || '',
          a: ant.a || '',
          alergias: ant.alergias || ''
        }
      });
    }

    // Cargar hábitos
    if (data.habitos) {
      const hab = data.habitos;
      this.nutriForm.patchValue({
        habitos: {
          fuma: hab.fuma || '',
          alcohol: hab.alcohol || '',
          cafe: hab.cafe || '',
          hidratacion: hab.hidratacion || '',
          gaseosas: hab.gaseosas || '',
          actividadFisica: hab.actividad_fisica || '',
          te: hab.te || '',
          edulcorantes: hab.edulcorantes || '',
          alimentacion: hab.alimentacion || ''
        }
      });
    }

    // Cargar valores bioquímicos
    if (data.valores_bioquimicos) {
      const bio = data.valores_bioquimicos;
      this.nutriForm.patchValue({
        valoresBioquimicos: {
          glicemia: bio.glicemia || '',
          colesterolTotal: bio.colesterol_total || '',
          trigliceridos: bio.trigliceridos || '',
          hdl: bio.hdl || '',
          ldl: bio.ldl || '',
          tgo: bio.tgo || '',
          tgp: bio.tgp || '',
          urea: bio.urea || '',
          creatinina: bio.creatinina || ''
        }
      });
    }

    // Cargar recordatorio 24h
    if (data.recordatorio_24h) {
      const rec = data.recordatorio_24h;
      this.nutriForm.patchValue({
        recordatorio24h: {
          desayuno: rec.desayuno || '',
          snack1: rec.snack1 || '',
          almuerzo: rec.almuerzo || '',
          snack2: rec.snack2 || '',
          cena: rec.cena || '',
          extras: rec.extras || ''
        }
      });
    }

    // Cargar frecuencia de consumo
    if (data.frecuencia_consumo && Array.isArray(data.frecuencia_consumo)) {
      this.frequencySelections = {};
      data.frecuencia_consumo.forEach((item: any) => {
        const key = this.frequencyKey(item.categoria, item.alimento);
        this.frequencySelections[key] = item.frecuencia;
      });
    }

    // Cargar notas extras
    this.nutriForm.patchValue({
      notasExtras: data.notas_extras || ''
    });

    console.log('Formulario después de cargar datos:', this.nutriForm.value);
    this.isEditingPatient.set(false);
  }

  private loadPacienteData(data: any): void {
    this.nutriForm.patchValue({
      personalData: {
        nombre: data.nombre || '',
        edadCronologica: data.edad_cronologica || '',
        sexo: data.sexo || '',
        lugarResidencia: data.lugar_residencia || '',
        estadoCivil: data.estado_civil || '',
        numeroCedula: data.numero_cedula || '',
        telefono: data.telefono || '',
        ocupacion: data.ocupacion || '',
        email: data.email || '',
        fechaConsulta: ''
      }
    });
    this.isEditingPatient.set(false);
  }

  protected submitHistory(): void {
    if (this.nutriForm.invalid) {
      this.nutriForm.markAllAsTouched();
    }

    this.isSubmitting.set(true);
    const formValue = this.nutriForm.value;
    
    // Transformar el payload para que coincida con lo que espera el backend
    const payload = {
      personalData: formValue.personalData,
      motivoConsulta: formValue.motivoConsulta,
      diagnostico: formValue.diagnostico,
      notasExtras: formValue.notasExtras,
      antecedentes: formValue.antecedentes,
      habitos: formValue.habitos,
      signosVitales: formValue.signos ? {
        presionArterial: formValue.signos.pa,
        temperatura: formValue.signos.temperatura,
        frecuenciaCardiaca: formValue.signos.fc,
        frecuenciaRespiratoria: formValue.signos.fr
      } : null,
      datosAntropometricos: formValue.antropometricos ? {
        edad: formValue.antropometricos.edad,
        edadMetabolica: formValue.antropometricos.edadMetabolica,
        sexo: formValue.antropometricos.sexo,
        peso: formValue.antropometricos.peso,
        masaMuscular: formValue.antropometricos.masaMuscular,
        grasaCorporalPorcentaje: formValue.antropometricos.gcPorc,
        grasaCorporal: formValue.antropometricos.gc,
        talla: formValue.antropometricos.talla,
        grasaVisceralPorcentaje: formValue.antropometricos.gvPorc,
        imc: formValue.antropometricos.imc,
        kcalBasales: formValue.antropometricos.kcalBasales,
        actividadFisica: formValue.antropometricos.actividadFisica,
        circunferenciaCintura: formValue.antropometricos.cintura,
        circunferenciaCadera: formValue.antropometricos.cadera,
        pantorrilla: formValue.antropometricos.pantorrilla,
        circunferenciaBrazo: formValue.antropometricos.cBrazo,
        circunferenciaMuslo: formValue.antropometricos.cMuslo,
        pesoAjustado: formValue.antropometricos.pesoAjustado,
        factorActividadFisica: formValue.antropometricos.factorActividadFisica,
        tiemposComida: formValue.antropometricos.tiemposComida
      } : null,
      valoresBioquimicos: formValue.valoresBioquimicos,
      recordatorio24h: formValue.recordatorio24h,
      frequency: this.frequencySelections
    };

    // Verificar si estamos en modo edición
    if (this.isEditMode() && this.editingId()) {
      if (this.editType() === 'paciente') {
        // Solo actualizar datos del paciente
        const pacienteData = {
          numeroCedula: payload.personalData.numeroCedula,
          nombre: payload.personalData.nombre,
          edadCronologica: payload.personalData.edadCronologica,
          sexo: payload.personalData.sexo,
          email: payload.personalData.email,
          telefono: payload.personalData.telefono,
          lugarResidencia: payload.personalData.lugarResidencia,
          estadoCivil: payload.personalData.estadoCivil,
          ocupacion: payload.personalData.ocupacion
        };
        
        this.nutritionService.updatePaciente(this.editingId()!, pacienteData).subscribe({
          next: () => {
            this.resultMessage.set('Datos del paciente actualizados correctamente.');
            this.isSubmitting.set(false);
            this.resetForm();
            // Navegar al dashboard después de guardar
            this.router.navigate(['/dashboard']);
          },
          error: (error) => {
            console.error(error);
            this.resultMessage.set(
              'No se pudo actualizar el paciente. Error: ' + (error.error?.error || error.message)
            );
            this.isSubmitting.set(false);
          }
        });
      } else {
        // Actualizar historia clínica completa
        console.log('Actualizando historia ID:', this.editingId());
        console.log('Payload a enviar:', payload);
        this.nutritionService.updateHistoriaClinica(this.editingId()!, payload).subscribe({
          next: () => {
            this.resultMessage.set('Historia clínica guardada correctamente.');
            this.isSubmitting.set(false);
            this.resetForm();
            // Navegar al dashboard después de guardar
            this.router.navigate(['/dashboard']);
          },
          error: (error) => {
            console.error('Error completo:', error);
            console.error('Error status:', error.status);
            console.error('Error response:', error.error);
            let errorMsg = 'No se pudo guardar la historia clínica.';
            if (error.status === 0) {
              errorMsg += ' No se puede conectar con el backend. Verifique que el servidor esté ejecutándose.';
            } else if (error.error?.error) {
              errorMsg += ' ' + error.error.error;
            } else if (error.error?.details) {
              errorMsg += ' Detalles: ' + error.error.details;
            } else if (error.message) {
              errorMsg += ' ' + error.message;
            }
            this.resultMessage.set(errorMsg);
            this.isSubmitting.set(false);
          }
        });
      }
    } else {
      // Crear nueva historia
      console.log('Creando nueva historia');
      console.log('Payload a enviar:', payload);
      this.nutritionService.saveRecord(payload).subscribe({
        next: () => {
          this.resultMessage.set('Historia guardada correctamente.');
          this.isSubmitting.set(false);
          this.resetForm();
        },
        error: (error) => {
          console.error('Error completo:', error);
          console.error('Error status:', error.status);
          console.error('Error response:', error.error);
          let errorMsg = 'No se pudo guardar la historia.';
          if (error.status === 0) {
            errorMsg += ' No se puede conectar con el backend. Verifique que el servidor esté ejecutándose.';
          } else if (error.error?.error) {
            errorMsg += ' ' + error.error.error;
          } else if (error.message) {
            errorMsg += ' ' + error.message;
          }
          this.resultMessage.set(errorMsg);
          this.isSubmitting.set(false);
        }
      });
    }
  }

  protected resetForm(): void {
    const wasEditMode = this.isEditMode();
    this.nutriForm.reset();
    this.frequencySelections = {};
    this.resultMessage.set(null);
    this.isEditingPatient.set(true);
    this.isEditMode.set(false);
    this.editingId.set(null);
    this.editType.set(null);
    this.pageTitle.set('NutriWeb · Historia clínica nutricional');
    
    // Si estaba en modo edición, navegar al dashboard
    if (wasEditMode) {
      this.router.navigate(['/dashboard']);
    }
  }

  protected toggleEditMode(): void {
    const isEditing = !this.isEditingPatient();
    this.isEditingPatient.set(isEditing);
    
    const personalDataGroup = this.nutriForm.get('personalData');
    if (personalDataGroup) {
      if (isEditing) {
        personalDataGroup.enable();
      } else {
        personalDataGroup.disable();
      }
    }
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
