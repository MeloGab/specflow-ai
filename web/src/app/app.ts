import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { SpecificationApi } from './specification-api';
import { AiProvider, SpecificationDocument } from './specification.model';

const exampleDemand =
  'Ao cancelar uma apólice antes de 7 dias, o cliente deve receber estorno integral. ' +
  'Após 7 dias, o estorno deve ser proporcional. Apólices com sinistro aberto não podem ser canceladas.';

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly api = inject(SpecificationApi);

  protected readonly demand = new FormControl(exampleDemand, {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(20), Validators.maxLength(5000)],
  });
  protected readonly loading = signal(false);
  protected readonly providersLoading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly result = signal<SpecificationDocument | null>(null);
  protected readonly providers = signal<AiProvider[]>([]);
  protected readonly selectedProviderId = signal('Ollama');

  ngOnInit(): void {
    const loginResult = new URLSearchParams(window.location.search).get('githubAuth');
    if (loginResult === 'success') {
      this.selectedProviderId.set('GitHubCopilot');
    } else if (loginResult === 'failed') {
      this.error.set('Não foi possível concluir o login com o GitHub. Tente novamente.');
    }

    if (loginResult) {
      window.history.replaceState({}, '', window.location.pathname);
    }

    void this.loadProviders();
  }

  protected selectProvider(provider: AiProvider): void {
    this.selectedProviderId.set(provider.id);
    this.result.set(null);
    this.error.set(null);
  }

  protected selectedProvider(): AiProvider | undefined {
    return this.providers().find(provider => provider.id === this.selectedProviderId());
  }

  protected canGenerate(): boolean {
    const provider = this.selectedProvider();
    return !!provider &&
      provider.available &&
      (!provider.requiresAuthentication || provider.authenticated);
  }

  protected loginWithGitHub(): void {
    window.location.assign('/api/auth/github/login');
  }

  protected async logoutGitHub(): Promise<void> {
    try {
      await firstValueFrom(this.api.logoutGitHub());
      await this.loadProviders();
    } catch (error: unknown) {
      this.error.set(this.describeError(error));
    }
  }

  protected async generate(): Promise<void> {
    this.demand.markAsTouched();
    const provider = this.selectedProvider();
    if (this.demand.invalid || this.loading() || !provider) {
      return;
    }

    if (!this.canGenerate()) {
      this.error.set(provider.message ?? 'Este provedor ainda não está disponível.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      this.result.set(await firstValueFrom(this.api.generate(
        this.demand.value.trim(),
        provider.id,
      )));
    } catch (error: unknown) {
      this.error.set(this.describeError(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadProviders(): Promise<void> {
    this.providersLoading.set(true);

    try {
      const providers = await firstValueFrom(this.api.providers());
      this.providers.set(providers);

      if (!providers.some(provider => provider.id === this.selectedProviderId())) {
        this.selectedProviderId.set(providers[0]?.id ?? 'Demo');
      }
    } catch (error: unknown) {
      this.error.set(this.describeError(error));
    } finally {
      this.providersLoading.set(false);
    }
  }

  private describeError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const validationErrors = error.error?.errors as Record<string, string[]> | undefined;
      const firstValidationError = validationErrors && Object.values(validationErrors).flat()[0];
      const problemDetail = error.error?.detail as string | undefined;
      return firstValidationError ?? problemDetail ??
        'Não foi possível gerar a especificação. Verifique se a API está ativa.';
    }

    return 'Ocorreu um erro inesperado ao gerar a especificação.';
  }
}
