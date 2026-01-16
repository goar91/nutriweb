import { Injectable, signal } from '@angular/core';

export interface EditContext {
  type: 'paciente' | 'historia';
  id: string;
  data?: any;
}

@Injectable({
  providedIn: 'root'
})
export class EditDataService {
  private editContext = signal<EditContext | null>(null);
  
  getEditContext() {
    return this.editContext();
  }

  setEditPaciente(id: string, data?: any) {
    this.editContext.set({ type: 'paciente', id, data });
  }

  setEditHistoria(id: string, data?: any) {
    this.editContext.set({ type: 'historia', id, data });
  }

  clearEditContext() {
    this.editContext.set(null);
  }
}
