import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NutritionService, UpdateUserProfileCommand, UserProfileResponse } from '../../core/services/nutrition.services';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  private nutritionService = inject(NutritionService);

  private readonly defaultProfile = {
    weight: 75,
    targetWeight: 75,
    height: 175,
    age: 30,
    gender: 'male',
    activityLevel: 'Moderado',
    goal: 'Mantener'
  };
  
  // Usamos un objeto reactivo para el formulario
  profile = signal({ ...this.defaultProfile });

  isSaving = signal(false);
  message = signal('');
  recordsCount = signal(0);
  isLoadingHistory = signal(false);
  historyStats = signal<Array<{ date: string; totalCalories: number }>>([]);

  historyChartPoints = computed(() => {
    const stats = this.historyStats().filter(item => item.totalCalories > 0);
    if (stats.length === 0) {
      return [] as Array<{ x: number; y: number }>;
    }

    const maxCalories = Math.max(...stats.map(item => item.totalCalories), 1);
    return stats.map((item, index) => {
      const x = stats.length === 1 ? 50 : 10 + (index * 80) / (stats.length - 1);
      const y = 80 - (item.totalCalories / maxCalories) * 54;
      return { x, y };
    });
  });

  historyPolylinePoints = computed(() => this.historyChartPoints().map(point => `${point.x},${point.y}`).join(' '));

  ngOnInit() {
    this.loadUserData();
    this.loadHistoryStats();
  }

  loadUserData() {
    this.nutritionService.getUserProfile().subscribe(data => {
      const mappedData = this.mapProfileFromBackend(data);
      this.profile.set({
        ...this.defaultProfile,
        ...mappedData
      });
    });
  }

  private mapProfileFromBackend(backendData: UserProfileResponse): any {
    // Mapear biologicalSex de backend (M/F) a frontend (male/female)
    const genderReverseMap: Record<string, string> = {
      'M': 'male',
      'F': 'female'
    };

    // Mapear activityLevel de número/enum backend a español UI
    const activityLevelReverseMap: Record<string | number, string> = {
      0: 'Sedentario',       // Sedentary
      1: 'Ligero',           // LightlyActive
      2: 'Moderado',         // ModeratelyActive
      3: 'Intenso',          // VeryActive
      4: 'ExtraActivo',      // ExtraActive
      'Sedentary': 'Sedentario',
      'LightlyActive': 'Ligero',
      'ModeratelyActive': 'Moderado',
      'VeryActive': 'Intenso',
      'ExtraActive': 'ExtraActivo'
    };

    return {
      weight: backendData.currentWeightKg ?? this.defaultProfile.weight,
      targetWeight: backendData.targetWeightKg ?? this.defaultProfile.targetWeight,
      height: backendData.heightCm ?? this.defaultProfile.height,
      age: backendData.age ?? this.defaultProfile.age,
      gender: genderReverseMap[backendData.biologicalSex] ?? this.defaultProfile.gender,
      activityLevel: activityLevelReverseMap[backendData.activityLevel] ?? this.defaultProfile.activityLevel,
      goal: backendData.goal ?? this.defaultProfile.goal
    };
  }

  saveProfile() {
    this.isSaving.set(true);
    const profileToSend = this.mapProfileForBackend(this.profile());
    this.nutritionService.updateProfile(profileToSend).subscribe({
      next: () => {
        this.message.set('¡Perfil y meta diaria actualizados!');
        this.isSaving.set(false);
        this.loadHistoryStats();
        setTimeout(() => this.message.set(''), 3000);
      },
      error: (err) => {
        console.error('Error al actualizar perfil:', err);
        this.message.set('Error al actualizar');
        this.isSaving.set(false);
      }
    });
  }

  private mapProfileForBackend(profile: any): UpdateUserProfileCommand {
    // Mapear gender de español a códigos esperados por backend (M/F)
    const genderMap: Record<string, string> = {
      'male': 'M',
      'female': 'F'
    };

    // Mapear activityLevel de español a número del enum del backend
    const activityLevelMap: Record<string, number> = {
      'Sedentario': 0,       // Sedentary
      'Ligero': 1,           // LightlyActive
      'Moderado': 2,         // ModeratelyActive
      'Intenso': 3,          // VeryActive
      'ExtraActivo': 4       // ExtraActive
    };

    // Calcular dailyCaloricTarget usando Harris-Benedict + activity multiplier
    const weight = Number(profile.weight) || this.defaultProfile.weight;
    const height = Number(profile.height) || this.defaultProfile.height;
    const age = Number(profile.age) || this.defaultProfile.age;
    const gender = profile.gender || 'male';

    return {
      weight,
      height,
      age,
      gender: genderMap[gender] || 'M',
      activityLevel: activityLevelMap[profile.activityLevel] ?? 2,
      goal: profile.goal || 'Mantener'
    };
  }

  resetProfile() {
    this.profile.set({ ...this.defaultProfile });
  }

  private loadHistoryStats(): void {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(endDate.getDate() - 29);

    this.isLoadingHistory.set(true);
    this.nutritionService.getStats(this.formatDate(startDate), this.formatDate(endDate)).subscribe({
      next: (data: any[]) => {
        const normalized = (data ?? []).map(item => ({
          date: item?.date ?? item?.Date ?? '',
          totalCalories: Number(item?.totalCalories ?? item?.TotalCalories ?? 0)
        }));

        this.historyStats.set(normalized);
        this.recordsCount.set(normalized.filter(item => item.totalCalories > 0).length);
        this.isLoadingHistory.set(false);
      },
      error: () => {
        this.historyStats.set([]);
        this.recordsCount.set(0);
        this.isLoadingHistory.set(false);
      }
    });
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  estimatedDailyGoal(): number {
    const data = this.profile();
    const weight = Number(data.weight) || this.defaultProfile.weight;
    const height = Number(data.height) || this.defaultProfile.height;
    const age = Number(data.age) || this.defaultProfile.age;

    const bmr = data.gender === 'female'
      ? (10 * weight) + (6.25 * height) - (5 * age) - 161
      : (10 * weight) + (6.25 * height) - (5 * age) + 5;

    const multiplierMap: Record<string, number> = {
      'Sedentario': 1.2,
      'Ligero': 1.375,
      'Moderado': 1.55,
      'Intenso': 1.725,
      'ExtraActivo': 1.9
    };

    const multiplier = multiplierMap[data.activityLevel] ?? 1.55;
    return Math.max(1200, Math.round(bmr * multiplier));
  }
}