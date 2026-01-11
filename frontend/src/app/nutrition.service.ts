import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NutritionService {
  private readonly baseUrl = `${environment.apiUrl}/nutrition`;

  constructor(private readonly http: HttpClient) {}

  saveRecord(payload: unknown) {
    return this.http.post(`${this.baseUrl}/history`, payload);
  }

  checkStatus() {
    return this.http.get(`${this.baseUrl}/status`);
  }
}
