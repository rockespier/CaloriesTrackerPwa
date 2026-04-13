import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NutritionService, HistoryDayResponse } from '../../core/services/nutrition.services';
import { RouterLink } from '@angular/router';

interface HistoryLogViewModel {
  text: string;
  calories: number;
  timestamp: string | Date;
}

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './history.component.html'
})
export class HistoryComponent implements OnInit {
  private nutritionService = inject(NutritionService);
  
  selectedDate = signal<string>(this.formatDate(new Date()));
  historyData = signal<HistoryDayResponse | null>(null);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string>('');
  historyLogs = computed<HistoryLogViewModel[]>(() => {
    const logs = this.historyData()?.logs ?? [];

    return logs.map((log: any) => ({
      text: log.OriginalText ?? log.originalText ?? log.text ?? log.description ?? log.foodText ?? log.mealDescription ?? log.name ?? 'Sin descripcion',
      calories: Number(log.EstimatedCalories ?? log.estimatedCalories ?? log.calories ?? log.totalCalories ?? log.kcal ?? 0),
      timestamp: log.LoggedAt ?? log.loggedAt ?? log.timestamp ?? log.createdAt ?? log.date ?? this.selectedDate()
    }));
  });

  ngOnInit(): void {
    this.loadHistory(this.selectedDate());
  }

  loadHistory(date: string) {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.selectedDate.set(date);
    
    console.log('📅 Cargando historial para:', date);
    
    this.nutritionService.getHistoryByDate(date).subscribe({
      next: (data) => {
        console.log('✅ Datos recibidos:', data);
        console.log('✅ Logs normalizados:', (data as any)?.logs);
        this.historyData.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('❌ Error cargando historial:', err);
        console.error('Status:', err.status);
        console.error('Mensaje:', err.error?.message || err.message);
        this.errorMessage.set(`Error: ${err.status} - ${err.error?.message || 'No hay datos disponibles para esta fecha'}`);
        this.isLoading.set(false);
      }
    });
  }

  changeDate(days: number) {
    const current = new Date(this.selectedDate());
    current.setDate(current.getDate() + days);
    this.loadHistory(this.formatDate(current));
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}