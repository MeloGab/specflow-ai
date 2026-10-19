import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AiProvider, SpecificationDocument } from './specification.model';

@Injectable({ providedIn: 'root' })
export class SpecificationApi {
  private readonly http = inject(HttpClient);

  providers(): Observable<AiProvider[]> {
    return this.http.get<AiProvider[]>('/api/providers');
  }

  generate(businessDemand: string, provider: string): Observable<SpecificationDocument> {
    return this.http.post<SpecificationDocument>('/api/specifications', {
      businessDemand,
      provider,
    });
  }

  logoutGitHub(): Observable<void> {
    return this.http.post<void>('/api/auth/github/logout', {});
  }
}
