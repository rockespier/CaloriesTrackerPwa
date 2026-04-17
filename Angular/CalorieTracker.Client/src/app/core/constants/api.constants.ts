import { environment } from '../../../environments/environment';

const normalizedBaseUrl = environment.apiBaseUrl.replace(/\/$/, '');
const normalizedApiVersionPath = `/${(environment.apiVersionPath || 'v1').replace(/^\/+|\/+$/g, '')}`;

export const API_BASE_URL = `${normalizedBaseUrl}${normalizedApiVersionPath}`;

export const API_ENDPOINTS = {
  users: `${API_BASE_URL}/users`,
  nutrition: `${API_BASE_URL}/nutrition`,
  activity: `${API_BASE_URL}/activity`
} as const;
