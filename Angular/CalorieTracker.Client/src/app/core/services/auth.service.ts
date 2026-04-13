import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_ENDPOINTS } from '../constants/api.constants';

// Definición de contratos DTO
export interface LoginCommand {
  email: string;
  passwordHash: string; // En frontend usamos el nombre genérico, se mapea a password
}

export interface RegisterCommand {
  email: string;
  passwordHash: string;
  name: string;
  heightCm: number;
  currentWeightKg: number;
  targetWeightKg: number;
  age: number;
  biologicalSex: string;
  activityLevel: number;
  goal: string;
}

export interface AuthResponse {
  token: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_ENDPOINTS.users;
  private readonly tokenKey = 'jwt_token';

  public login(command: LoginCommand): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, {
      email: command.email,
      password: command.passwordHash
    }).pipe(
      tap(response => this.saveToken(response.token))
    );
  }

  public register(command: RegisterCommand): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, {
      ...command,
      password: command.passwordHash
    });
  }

  private saveToken(token: string): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(this.tokenKey, token);
    }
  }

  public getToken(): string | null {
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem(this.tokenKey);
    }
    return null;
  }

  public logout(): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(this.tokenKey);
    }
  }

  public isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
