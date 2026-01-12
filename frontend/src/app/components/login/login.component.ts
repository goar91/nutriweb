import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="login-container">
      <div class="login-card">
        <div class="login-header">
          <div class="logo">
            <svg width="48" height="48" viewBox="0 0 48 48" fill="none">
              <circle cx="24" cy="24" r="20" fill="url(#gradient)" />
              <path d="M24 12v24M16 24h16" stroke="white" stroke-width="3" stroke-linecap="round"/>
              <defs>
                <linearGradient id="gradient" x1="0" y1="0" x2="48" y2="48">
                  <stop offset="0%" stop-color="#667eea" />
                  <stop offset="100%" stop-color="#764ba2" />
                </linearGradient>
              </defs>
            </svg>
          </div>
          <h1>NutriWeb</h1>
          <p>Sistema de Historias Clínicas Nutricionales</p>
        </div>

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
          @if (errorMessage()) {
            <div class="error-alert">
              <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"/>
              </svg>
              {{ errorMessage() }}
            </div>
          }

          <div class="form-group">
            <label for="username">Usuario</label>
            <input
              id="username"
              type="text"
              formControlName="username"
              placeholder="Ingrese su usuario"
              [class.invalid]="loginForm.get('username')?.invalid && loginForm.get('username')?.touched"
            />
            @if (loginForm.get('username')?.invalid && loginForm.get('username')?.touched) {
              <span class="error-text">El usuario es requerido</span>
            }
          </div>

          <div class="form-group">
            <label for="password">Contraseña</label>
            <div class="password-input">
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                placeholder="Ingrese su contraseña"
                [class.invalid]="loginForm.get('password')?.invalid && loginForm.get('password')?.touched"
              />
              <button
                type="button"
                class="toggle-password"
                (click)="togglePassword()"
                tabindex="-1"
              >
                @if (showPassword()) {
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                    <path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/>
                    <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z"/>
                  </svg>
                } @else {
                  <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074l-1.78-1.781zm4.261 4.26l1.514 1.515a2.003 2.003 0 012.45 2.45l1.514 1.514a4 4 0 00-5.478-5.478z"/>
                    <path d="M12.454 16.697L9.75 13.992a4 4 0 01-3.742-3.741L2.335 6.578A9.98 9.98 0 00.458 10c1.274 4.057 5.065 7 9.542 7 .847 0 1.669-.105 2.454-.303z"/>
                  </svg>
                }
              </button>
            </div>
            @if (loginForm.get('password')?.invalid && loginForm.get('password')?.touched) {
              <span class="error-text">La contraseña es requerida</span>
            }
          </div>

          <div class="form-group-checkbox">
            <input
              id="remember"
              type="checkbox"
              formControlName="rememberMe"
            />
            <label for="remember">Recordar mi sesión</label>
          </div>

          <button
            type="submit"
            class="btn-login"
            [disabled]="loginForm.invalid || isLoading()"
          >
            @if (isLoading()) {
              <span class="spinner"></span>
              Iniciando sesión...
            } @else {
              Iniciar Sesión
            }
          </button>
        </form>

        <div class="register-section">
          <div class="section-title">Crear cuenta</div>
          <form [formGroup]="registerForm" (ngSubmit)="onRegister()" class="register-form">
            @if (registerError()) {
              <div class="error-alert">
                <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"/>
                </svg>
                {{ registerError() }}
              </div>
            }

            <div class="form-group">
              <label for="register-username">Usuario</label>
              <input
                id="register-username"
                type="text"
                formControlName="username"
                placeholder="Crea tu usuario"
                [class.invalid]="registerForm.get('username')?.invalid && registerForm.get('username')?.touched"
              />
              @if (registerForm.get('username')?.invalid && registerForm.get('username')?.touched) {
                <span class="error-text">El usuario es requerido</span>
              }
            </div>

            <div class="form-group">
              <label for="register-password">Contraseña</label>
              <div class="password-input">
                <input
                  id="register-password"
                  [type]="showRegisterPassword() ? 'text' : 'password'"
                  formControlName="password"
                  placeholder="Crea tu contraseña"
                  [class.invalid]="registerForm.get('password')?.invalid && registerForm.get('password')?.touched"
                />
                <button
                  type="button"
                  class="toggle-password"
                  (click)="toggleRegisterPassword()"
                  tabindex="-1"
                >
                  @if (showRegisterPassword()) {
                    <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                      <path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/>
                      <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z"/>
                    </svg>
                  } @else {
                    <svg width="20" height="20" viewBox="0 0 20 20" fill="currentColor">
                      <path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074l-1.78-1.781zm4.261 4.26l1.514 1.515a2.003 2.003 0 012.45 2.45l1.514 1.514a4 4 0 00-5.478-5.478z"/>
                      <path d="M12.454 16.697L9.75 13.992a4 4 0 01-3.742-3.741L2.335 6.578A9.98 9.98 0 00.458 10c1.274 4.057 5.065 7 9.542 7 .847 0 1.669-.105 2.454-.303z"/>
                    </svg>
                  }
                </button>
              </div>
              @if (registerForm.get('password')?.invalid && registerForm.get('password')?.touched) {
                <span class="error-text">La contraseña es requerida</span>
              }
            </div>

            <div class="form-group">
              <label for="register-email">Email (opcional)</label>
              <input
                id="register-email"
                type="email"
                formControlName="email"
                placeholder="correo@ejemplo.com"
                [class.invalid]="registerForm.get('email')?.invalid && registerForm.get('email')?.touched"
              />
              @if (registerForm.get('email')?.invalid && registerForm.get('email')?.touched) {
                <span class="error-text">El email no es válido</span>
              }
            </div>

            <button
              type="submit"
              class="btn-register"
              [disabled]="registerForm.invalid || isRegistering()"
            >
              @if (isRegistering()) {
                <span class="spinner"></span>
                Creando cuenta...
              } @else {
                Registrarme
              }
            </button>
          </form>
        </div>

        <div class="login-footer">
          <p>© 2024 NutriWeb. Todos los derechos reservados.</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-container {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      padding: 1rem;
    }

    .login-card {
      background: white;
      border-radius: 1rem;
      box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
      width: 100%;
      max-width: 440px;
      overflow: hidden;
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .login-header {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      padding: 3rem 2rem 2rem;
      text-align: center;
    }

    .logo {
      margin-bottom: 1rem;
    }

    .login-header h1 {
      font-size: 2rem;
      font-weight: 700;
      margin: 0 0 0.5rem;
    }

    .login-header p {
      font-size: 0.95rem;
      opacity: 0.95;
      margin: 0;
    }

    .login-form {
      padding: 2rem;
    }

    .register-section {
      border-top: 1px solid #e6e6e6;
      padding: 1.5rem 2rem 2rem;
      background: #fafbff;
    }

    .section-title {
      font-size: 0.85rem;
      font-weight: 700;
      letter-spacing: 0.12em;
      text-transform: uppercase;
      color: #5a5a5a;
      margin-bottom: 1rem;
    }

    .register-form {
      display: flex;
      flex-direction: column;
    }

    .btn-register {
      width: 100%;
      padding: 0.95rem;
      background: white;
      color: #667eea;
      border: 2px solid #667eea;
      border-radius: 0.5rem;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s, background 0.2s, color 0.2s;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
    }

    .btn-register:hover:not(:disabled) {
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      box-shadow: 0 8px 20px rgba(102, 126, 234, 0.25);
      transform: translateY(-1px);
    }

    .btn-register:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .btn-register .spinner {
      border-color: rgba(102, 126, 234, 0.3);
      border-top-color: #667eea;
    }

    .error-alert {
      background: #fee;
      color: #c33;
      padding: 0.875rem 1rem;
      border-radius: 0.5rem;
      margin-bottom: 1.5rem;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      font-size: 0.9rem;
      border: 1px solid #fcc;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-group label {
      display: block;
      margin-bottom: 0.5rem;
      font-weight: 600;
      color: #333;
      font-size: 0.95rem;
    }

    .form-group input {
      width: 100%;
      padding: 0.875rem 1rem;
      border: 2px solid #e0e0e0;
      border-radius: 0.5rem;
      font-size: 1rem;
      transition: all 0.2s;
      box-sizing: border-box;
    }

    .form-group input:focus {
      outline: none;
      border-color: #667eea;
      box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
    }

    .form-group input.invalid {
      border-color: #f44336;
    }

    .password-input {
      position: relative;
    }

    .password-input input {
      padding-right: 3rem;
    }

    .toggle-password {
      position: absolute;
      right: 0.75rem;
      top: 50%;
      transform: translateY(-50%);
      background: none;
      border: none;
      color: #666;
      cursor: pointer;
      padding: 0.5rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: color 0.2s;
    }

    .toggle-password:hover {
      color: #667eea;
    }

    .error-text {
      display: block;
      color: #f44336;
      font-size: 0.85rem;
      margin-top: 0.375rem;
    }

    .form-group-checkbox {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1.5rem;
    }

    .form-group-checkbox input[type="checkbox"] {
      width: 1.125rem;
      height: 1.125rem;
      cursor: pointer;
    }

    .form-group-checkbox label {
      cursor: pointer;
      user-select: none;
      color: #666;
      font-size: 0.9rem;
    }

    .btn-login {
      width: 100%;
      padding: 1rem;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
      border: none;
      border-radius: 0.5rem;
      font-size: 1rem;
      font-weight: 600;
      cursor: pointer;
      transition: transform 0.2s, box-shadow 0.2s;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
    }

    .btn-login:hover:not(:disabled) {
      transform: translateY(-2px);
      box-shadow: 0 8px 20px rgba(102, 126, 234, 0.4);
    }

    .btn-login:active:not(:disabled) {
      transform: translateY(0);
    }

    .btn-login:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .spinner {
      width: 1rem;
      height: 1rem;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .login-footer {
      padding: 1.5rem 2rem;
      background: #f8f9fa;
      text-align: center;
      border-top: 1px solid #e0e0e0;
    }

    .login-footer p {
      margin: 0;
      color: #666;
      font-size: 0.85rem;
    }

    @media (max-width: 480px) {
      .login-card {
        border-radius: 0;
      }

      .login-header {
        padding: 2rem 1.5rem 1.5rem;
      }

      .login-form {
        padding: 1.5rem;
      }

      .register-section {
        padding: 1.5rem;
      }

      .login-footer {
        padding: 1rem 1.5rem;
      }
    }
  `]
})
export class LoginComponent {
  loginForm: FormGroup;
  registerForm: FormGroup;
  isLoading = signal(false);
  isRegistering = signal(false);
  errorMessage = signal('');
  registerError = signal('');
  showPassword = signal(false);
  showRegisterPassword = signal(false);

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
      rememberMe: [false]
    });
    this.registerForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
      email: ['', Validators.email]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const { username, password } = this.loginForm.value;

    this.authService.login(username, password).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success) {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (error) => {
        this.isLoading.set(false);
        if (error.status === 401) {
          this.errorMessage.set('Usuario o contrase?a incorrectos');
        } else {
          this.errorMessage.set('Error al iniciar sesi?n. Por favor, intente nuevamente.');
        }
      }
    });
  }

  togglePassword(): void {
    this.showPassword.set(!this.showPassword());
  }

  onRegister(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isRegistering.set(true);
    this.registerError.set('');

    const { username, password, email } = this.registerForm.value;
    const emailValue = typeof email === 'string' && email.trim().length > 0 ? email.trim() : undefined;

    this.authService.register(username, password, emailValue).subscribe({
      next: (response) => {
        this.isRegistering.set(false);
        if (response.success) {
          this.registerForm.reset();
          this.router.navigate(['/dashboard']);
        } else {
          this.registerError.set('No se pudo completar el registro.');
        }
      },
      error: (error) => {
        this.isRegistering.set(false);
        if (error.status === 409) {
          this.registerError.set('El usuario o email ya existe.');
        } else {
          this.registerError.set('Error al registrar. Intente nuevamente.');
        }
      }
    });
  }

  toggleRegisterPassword(): void {
    this.showRegisterPassword.set(!this.showRegisterPassword());
  }
}
