export const fallbackBaseURL = 'http://127.0.0.1:3000';

export function resolveBaseUrl(candidate?: string | null): string {
  const trimmed = candidate?.trim();

  if (trimmed && trimmed.length > 0) {
    return trimmed.includes('://') ? trimmed : `http://${trimmed}`;
  }

  return fallbackBaseURL;
}
