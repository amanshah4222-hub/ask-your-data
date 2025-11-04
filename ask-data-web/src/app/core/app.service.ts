import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);

  // point this to your Zeabur API:
  private baseUrl = 'https://ask.zeabur.app';

  ask(question: string, limit = 10) {
    return this.http.post<{ data: any[]; explain: any }>(
      `${this.baseUrl}/ask`,
      { question, limit }
    );
  }

  me() {
    return this.http.get(`${this.baseUrl}/me`);
  }
}