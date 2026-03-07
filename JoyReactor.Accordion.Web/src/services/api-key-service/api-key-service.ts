import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ApiKeyService {
  get(): string | null {
    const apiKey = localStorage.getItem('apiKey');
    return apiKey;
  }

  set(apiKey: string) {
    localStorage.setItem('apiKey', apiKey);
  }
}