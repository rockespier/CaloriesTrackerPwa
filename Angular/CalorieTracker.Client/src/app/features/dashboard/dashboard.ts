import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { NutritionService, FoodLogEntry, NutritionStatsResponse, WeeklyStatusResponse, WeeklySummaryResponse } from '../../core/services/nutrition.services';
import { Router, RouterLink } from '@angular/router';

interface ChartPoint {
  date: string;
  label: string;
  totalCalories: number;
  x: number;
  y: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.html'
})
export class DashboardComponent implements OnInit {
  private authService = inject(AuthService);
  private nutritionService = inject(NutritionService);
  private router = inject(Router);
  private readonly chartWidth = 320;
  private readonly chartHeight = 180;
  private readonly chartPadding = { top: 16, right: 16, bottom: 28, left: 16 };

// Estado reactivo (Signals)
  dailyCaloricTarget = signal<number>(2200); 
  caloriesConsumed = signal<number>(0);
  foodInputText = signal<string>('');
  isListening = signal<boolean>(false);
  isProcessing = signal<boolean>(false);
  
  // Señales de historial y estado
  foodLogs = signal<FoodLogEntry[]>([]);
  errorMessage = signal<string>('');

  weeklyStatus = signal<WeeklyStatusResponse | null>(null);
  isLoadingWeekly = signal<boolean>(false);
  rangeStats = signal<NutritionStatsResponse[]>([]);
  isLoadingStats = signal<boolean>(false);
  hoveredPoint = signal<ChartPoint | null>(null);

  chartStats = computed(() => [...this.rangeStats()].sort((a, b) => a.date.localeCompare(b.date)));
  chartMaxCalories = computed(() => Math.max(...this.chartStats().map(item => item.totalCalories), 1));
  chartPoints = computed<ChartPoint[]>(() => {
    const stats = this.chartStats();
    if (stats.length === 0) {
      return [];
    }

    const plotWidth = this.chartWidth - this.chartPadding.left - this.chartPadding.right;
    const plotHeight = this.chartHeight - this.chartPadding.top - this.chartPadding.bottom;
    const maxCalories = this.chartMaxCalories();

    return stats.map((item, index) => {
      const x = stats.length === 1
        ? this.chartPadding.left + plotWidth / 2
        : this.chartPadding.left + (index * plotWidth) / (stats.length - 1);
      const y = this.chartPadding.top + plotHeight - (item.totalCalories / maxCalories) * plotHeight;

      return {
        date: item.date,
        label: this.formatShortDate(item.date),
        totalCalories: item.totalCalories,
        x,
        y
      };
    });
  });
  chartLinePath = computed(() => {
    const points = this.chartPoints();
    if (points.length === 0) {
      return '';
    }

    return points
      .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`)
      .join(' ');
  });
  chartAreaPath = computed(() => {
    const points = this.chartPoints();
    if (points.length === 0) {
      return '';
    }

    const baseline = this.chartHeight - this.chartPadding.bottom;
    const firstPoint = points[0];
    const lastPoint = points[points.length - 1];
    const line = points.map(point => `L ${point.x} ${point.y}`).join(' ');

    return `M ${firstPoint.x} ${baseline} ${line} L ${lastPoint.x} ${baseline} Z`;
  });
  chartAverageCalories = computed(() => {
    const stats = this.chartStats();
    if (stats.length === 0) {
      return 0;
    }

    const total = stats.reduce((sum, item) => sum + item.totalCalories, 0);
    return Math.round(total / stats.length);
  });
  
  private recognition: any;

  ngOnInit(): void {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return;
    }
    this.initSpeechRecognition();
    // 1. Cargar el total del dia actual desde endpoints soportados por backend.
    this.loadTodayCalories();

    // 2. Cargar resumen semanal desde endpoint dedicado.
    this.loadWeeklySummary();

    // 3. Cargar stats para el grafico de 7 dias.
    this.loadRangeStats();
  }

  loadWeeklySummary() {
    this.isLoadingWeekly.set(true);
    this.nutritionService.getWeeklySummary().subscribe({
      next: (data: WeeklySummaryResponse) => {
        const normalized = this.normalizeWeeklyStatus(data);
        this.weeklyStatus.set(normalized);
        if (normalized.dailyTarget > 0) {
          this.dailyCaloricTarget.set(normalized.dailyTarget);
        }
        this.isLoadingWeekly.set(false);
      },
      error: (err) => {
        console.error('Error cargando resumen semanal', err);
        this.isLoadingWeekly.set(false);
        this.populateWeeklyStatusFromStats();
      }
    });
  }

  private loadTodayCalories(): void {
    const now = new Date();
    const localDate = this.formatDate(now);
    const utcDate = now.toISOString().split('T')[0];

    this.nutritionService.getDailyTotal(localDate).subscribe({
      next: (dailyTotal) => {
        const localTotal = this.parseTotalCalories(dailyTotal);
        this.resolveTodayCaloriesWithFallback(localDate, utcDate, localTotal);
      },
      error: (err) => {
        console.error('Error cargando total diario', err);
        this.resolveTodayCaloriesWithFallback(localDate, utcDate, 0);
      }
    });
  }

  private resolveTodayCaloriesWithFallback(localDate: string, utcDate: string, localTotal: number): void {
    if (utcDate !== localDate) {
      this.nutritionService.getDailyTotal(utcDate).subscribe({
        next: (utcDailyTotal) => {
          const utcTotal = this.parseTotalCalories(utcDailyTotal);
          this.caloriesConsumed.set(Math.max(localTotal, utcTotal));
        },
        error: () => {
          this.caloriesConsumed.set(localTotal);
        }
      });
      return;
    }

    this.caloriesConsumed.set(localTotal);
  }

  loadRangeStats() {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(endDate.getDate() - 6);

    this.isLoadingStats.set(true);
    this.nutritionService
      .getStats(this.formatDate(startDate), this.formatDate(endDate))
      .subscribe({
        next: (data) => {
          this.rangeStats.set(
            data.map((item: any) => ({
              date: item.date ?? item.Date,
              totalCalories: Number(item.totalCalories ?? item.TotalCalories ?? 0)
            }))
          );
          if (!this.weeklyStatus()) {
            this.populateWeeklyStatusFromStats();
          }
          this.isLoadingStats.set(false);
        },
        error: (err) => {
          console.error('Error cargando stats', err);
          this.isLoadingStats.set(false);
        }
      });
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private parseTotalCalories(response: any): number {
    const raw = response?.totalCalories ?? response?.TotalCalories ?? response?.total ?? response?.Total ?? 0;
    return Number(raw) || 0;
  }

  private parseHistoryTotal(response: any): number {
    const declaredTotal = Number(response?.totalCalories ?? response?.TotalCalories ?? response?.total ?? response?.Total ?? NaN);
    if (!Number.isNaN(declaredTotal)) {
      return declaredTotal;
    }

    const logs = response?.logs ?? response?.Logs ?? [];
    if (!Array.isArray(logs)) {
      return 0;
    }

    return logs.reduce((sum: number, log: any) => {
      const value = Number(log?.estimatedCalories ?? log?.EstimatedCalories ?? log?.calories ?? log?.Calories ?? 0);
      return sum + (Number.isNaN(value) ? 0 : value);
    }, 0);
  }

  private normalizeWeeklyStatus(data: any): WeeklyStatusResponse {
    const dailyStats = data?.dailyStats ?? data?.DailyStats ?? [];
    const consumedFromDailyStats = Array.isArray(dailyStats)
      ? dailyStats.reduce((sum: number, day: any) => sum + Number(day?.totalCalories ?? day?.TotalCalories ?? 0), 0)
      : 0;

    const weeklyConsumed = Number(data?.weeklyConsumed ?? data?.WeeklyConsumed ?? data?.totalCalories ?? data?.TotalCalories ?? data?.consumed ?? data?.Consumed ?? consumedFromDailyStats ?? 0);
    const dailyTarget = Number(data?.dailyTarget ?? data?.DailyTarget ?? this.dailyCaloricTarget());
    const weeklyTarget = Number(data?.weeklyTarget ?? data?.WeeklyTarget ?? data?.target ?? data?.Target ?? dailyTarget * 7);
    const daysActive = Number(data?.daysActive ?? data?.DaysActive ?? data?.activeDays ?? data?.ActiveDays ?? (Array.isArray(dailyStats) ? dailyStats.filter((day: any) => Number(day?.totalCalories ?? day?.TotalCalories ?? 0) > 0).length : 0));
    const complianceStatusRaw = data?.complianceStatus ?? data?.ComplianceStatus ?? data?.status ?? data?.Status;
    const complianceStatus = complianceStatusRaw
      ? String(complianceStatusRaw)
      : weeklyConsumed <= weeklyTarget
        ? 'En Meta'
        : 'Fuera de Meta';

    return {
      dailyTarget,
      weeklyConsumed,
      weeklyTarget,
      complianceStatus,
      daysActive
    };
  }

  private populateWeeklyStatusFromStats(): void {
    const stats = this.rangeStats();
    if (stats.length === 0) {
      return;
    }

    const daysActive = stats.filter(item => item.totalCalories > 0).length;
    const weeklyConsumed = stats.reduce((sum, item) => sum + item.totalCalories, 0);
    const weeklyTarget = this.dailyCaloricTarget() * 7;

    this.weeklyStatus.set({
      dailyTarget: this.dailyCaloricTarget(),
      weeklyConsumed,
      weeklyTarget,
      complianceStatus: weeklyConsumed <= weeklyTarget ? 'En Meta' : 'Fuera de Meta',
      daysActive
    });
  }

  weeklyProgressWidth(): number {
    return Math.min(((this.weeklyStatus()?.daysActive ?? 0) / 7) * 100, 100);
  }

  weeklyStatusLabel(): string {
    if (this.isLoadingWeekly()) {
      return 'Cargando estado semanal...';
    }

    return this.weeklyStatus()?.complianceStatus || 'Sin datos semanales';
  }

  weeklyStatusMessage(): string {
    const status = (this.weeklyStatus()?.complianceStatus || '').toLowerCase();
    if (!status) {
      return 'Aun no hay suficientes datos para evaluar tu semana.';
    }

    if (status.includes('meta')) {
      return 'Vas por buen camino para alcanzar tu peso deseado.';
    }

    return 'Has excedido tu meta semanal. Manana es una nueva oportunidad.';
  }

  formatWeekdayShort(date: string): string {
    return new Intl.DateTimeFormat('es-ES', {
      weekday: 'short'
    }).format(new Date(date));
  }

  private formatShortDate(date: string): string {
    return new Intl.DateTimeFormat('es-ES', {
      day: '2-digit',
      month: 'short'
    }).format(new Date(date));
  }

  formatLongDate(date: string): string {
    return new Intl.DateTimeFormat('es-ES', {
      weekday: 'long',
      day: '2-digit',
      month: 'short'
    }).format(new Date(date));
  }

  isAboveTarget(calories: number): boolean {
    return calories > this.dailyCaloricTarget();
  }

  getPointStroke(calories: number): string {
    return this.isAboveTarget(calories) ? '#ef4444' : '#10b981';
  }

  getPointFill(calories: number): string {
    return this.isAboveTarget(calories) ? '#fef2f2' : '#ecfdf5';
  }

  showTooltip(point: ChartPoint): void {
    this.hoveredPoint.set(point);
  }

  hideTooltip(): void {
    this.hoveredPoint.set(null);
  }

 
  get caloriesRemaining(): number {
    return Math.max(0, this.dailyCaloricTarget() - this.caloriesConsumed());
  }

  get progressPercentage(): number {
    const percentage = (this.caloriesConsumed() / this.dailyCaloricTarget()) * 100;
    return Math.min(percentage, 100);
  }

  private initSpeechRecognition(): void {
    const SpeechRecognitionAPI = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;
    
    if (SpeechRecognitionAPI) {
      this.recognition = new SpeechRecognitionAPI();
      this.recognition.continuous = false;
      this.recognition.lang = 'es-ES';
      this.recognition.interimResults = false;

      this.recognition.onresult = (event: any) => {
        const transcript = event.results[0][0].transcript;
        this.foodInputText.set(transcript);
        this.isListening.set(false);
        this.processFoodEntry();
      };

      this.recognition.onerror = (event: any) => {
        console.error('Error de voz:', event.error);
        this.isListening.set(false);
      };

      this.recognition.onend = () => {
        this.isListening.set(false);
      };
    }
  }

  toggleListening(): void {
    if (this.isListening()) {
      this.recognition.stop();
      this.isListening.set(false);
    } else {
      try {
        this.recognition.start();
        this.isListening.set(true);
      } catch (e) {
        console.error('El reconocimiento ya ha comenzado.', e);
      }
    }
  }

  processFoodEntry(): void {
    const text = this.foodInputText().trim();
    if (!text) return;

    this.isProcessing.set(true);
    this.errorMessage.set('');

    this.nutritionService.logFood(text).subscribe({
      next: (response) => {
        const calories = Number((response as any)?.calories ?? (response as any)?.Calories ?? 0);
        const normalizedCalories = Number.isFinite(calories) ? calories : 0;

        // Actualizamos el total de calorías
        this.caloriesConsumed.update(c => c + normalizedCalories);
        
        // Agregamos al historial (al inicio de la lista)
        this.foodLogs.update(logs => [{
          text: text,
          calories: normalizedCalories,
          timestamp: new Date()
        }, ...logs]);

        this.foodInputText.set('');
        // Refresca panel de 7 dias y estado semanal con datos persistidos.
        this.loadRangeStats();
        this.loadWeeklySummary();
        this.isProcessing.set(false);
      },
      error: (err) => {
        console.error('Error al registrar alimento', err);
        const backendDetail = err?.error?.detail || err?.error?.message || err?.message;
        this.errorMessage.set(backendDetail || 'No pudimos procesar este alimento. Intenta de nuevo.');
        this.isProcessing.set(false);
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}