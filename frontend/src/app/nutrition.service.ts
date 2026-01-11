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
}
