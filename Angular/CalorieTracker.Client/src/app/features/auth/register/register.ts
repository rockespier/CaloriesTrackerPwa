import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Z])(?=.*\d).*$/)]],
    name: ['', [Validators.required]],
    heightCm: [170, [Validators.required, Validators.min(100), Validators.max(250)]],
    currentWeightKg: [70, [Validators.required, Validators.min(30), Validators.max(300)]],
    targetWeightKg: [65, [Validators.required, Validators.min(30), Validators.max(300)]],
    age: [25, [Validators.required, Validators.min(15), Validators.max(120)]],
    biologicalSex: ['M', [Validators.required]],
    activityLevel: [0, [Validators.required]], // 0: Sedentario, 1: Ligero, 2: Moderado, 3: Activo, 4: Muy Activo
    goal: ['Mantener', [Validators.required]] // Perder | Ganar | Mantener
  });

  errorMessage = '';
  isLoading = false;

  get uppercaseOk(): boolean { return /[A-Z]/.test(this.registerForm.controls.password.value); }
  get numberOk(): boolean    { return /\d/.test(this.registerForm.controls.password.value); }

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';

    const formValues = this.registerForm.getRawValue();

    this.authService.register({
      email: formValues.email,
      passwordHash: formValues.password,
      name: formValues.name,
      heightCm: Number(formValues.heightCm),
      currentWeightKg: Number(formValues.currentWeightKg),
      targetWeightKg: Number(formValues.targetWeightKg),
      age: Number(formValues.age),
      biologicalSex: formValues.biologicalSex,
      activityLevel: Number(formValues.activityLevel),
      goal: formValues.goal
    }).subscribe({
      next: () => {
        // Post-registro exitoso, enviamos al login
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Error al registrar usuario. Verifica tus datos.';
      }
    });
  }
}