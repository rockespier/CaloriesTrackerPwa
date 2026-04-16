import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from '../constants/api.constants';

export interface LogFoodResponse {
  calories: number;
  message: string;
}

export interface FoodLogEntry {
  text: string;
  calories: number;
  timestamp: Date;
}

export interface WeeklyStatusResponse {
  dailyTarget: number;
  weeklyConsumed: number;
  weeklyTarget: number;
  complianceStatus: string;
  daysActive: number | 1;
}

export interface HistoryDayResponse {
  date: string;
  logs: FoodLogEntry[];
  totalCalories: number;
}

export interface NutritionStatsResponse {
  date: string;
  totalCalories: number;
}

export interface UserProfileResponse {
  currentWeightKg: number;
  targetWeightKg?: number;
  heightCm: number;
  age: number;
  biologicalSex: string;
  activityLevel: number | string;
  goal: string;
  dailyCaloricTarget: number;
}

export interface UpdateUserProfileCommand {
  weight: number;
  height: number;
  age: number;
  gender: string;
  activityLevel: number;
  goal: string;
}

export interface UpdateProfileResponse {
  success: boolean;
  message: string;
  newTarget: number;
}

export interface DailyTotalResponse {
  date: string;
  totalCalories: number;
}

// ─── Actividad Física ────────────────────────────────────────────────────────

export interface LogActivityResponse {
  caloriesBurned: number;
  message: string;
}

export interface ActivityLogEntry {
  id: string;
  activityDescription: string;
  durationMinutes: number;
  caloriesBurned: number;
  loggedAt: Date;
}

export interface ActivityHistoryResponse {
  date: string;
  activities: ActivityLogEntry[];
  totalCaloriesBurned: number;
}

export interface DailyBurnedResponse {
  date: string;
  totalCaloriesBurned: number;
}

export interface WeeklySummaryResponse {
  startDate: string;
  endDate: string;
  dailyStats: Array<{ date: string; totalCalories: number; mealCount: number }>;
  totalCalories: number;
  averageCalories: number;
  dailyTarget: number;
  averageDifference: number;
}

@Injectable({
  providedIn: 'root'
})
export class NutritionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrlNutrition = API_ENDPOINTS.nutrition;
  private readonly apiUrlUsers = API_ENDPOINTS.users;
  private readonly apiUrlActivity = API_ENDPOINTS.activity;

  public logFood(text: string): Observable<LogFoodResponse> {
    return this.http.post<LogFoodResponse>(`${this.apiUrlNutrition}/log`, { text });
  }
  public getDailyTotal(date: string): Observable<DailyTotalResponse> {
    return this.http.get<DailyTotalResponse>(`${this.apiUrlNutrition}/daily-total?date=${date}`);
  }

  public getWeeklySummary(): Observable<WeeklySummaryResponse> {
    return this.http.get<WeeklySummaryResponse>(`${this.apiUrlNutrition}/weekly-summary`);
  }

  public getHistoryByDate(date: string): Observable<HistoryDayResponse> {
  return this.http.get<HistoryDayResponse>(`${this.apiUrlNutrition}/history/${date}`);
  }

  public getStats(start: string, end: string): Observable<NutritionStatsResponse[]> {
    return this.http.get<NutritionStatsResponse[]>(`${this.apiUrlNutrition}/stats?startDate=${start}&endDate=${end}`);
  }
  /**
   * Obtiene los datos bio-métricos actuales del usuario autenticado
   */
  public getUserProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(`${this.apiUrlUsers}/profile`);
  }

  /**
   * Envía los nuevos datos al servidor, actualiza el histórico
   * y retorna la nueva meta calórica calculada.
   */
  public updateProfile(profile: UpdateUserProfileCommand): Observable<UpdateProfileResponse> {
    return this.http.put<UpdateProfileResponse>(`${this.apiUrlUsers}/profile`, profile);
  }

  // ─── Actividad Física ──────────────────────────────────────────────────────

  /**
   * Registra una actividad física y retorna las calorías quemadas (via Gemini).
   */
  public logActivity(activityDescription: string, durationMinutes: number): Observable<LogActivityResponse> {
    return this.http.post<LogActivityResponse>(`${this.apiUrlActivity}/log`, {
      activityDescription,
      durationMinutes
    });
  }

  /**
   * Obtiene el historial de actividades físicas de un día específico.
   */
  public getActivityHistory(date: string): Observable<ActivityHistoryResponse> {
    return this.http.get<ActivityHistoryResponse>(`${this.apiUrlActivity}/history/${date}`);
  }

  /**
   * Obtiene el total de calorías quemadas en un día específico.
   */
  public getDailyBurned(date: string): Observable<DailyBurnedResponse> {
    return this.http.get<DailyBurnedResponse>(`${this.apiUrlActivity}/daily-burned?date=${date}`);
  }
}