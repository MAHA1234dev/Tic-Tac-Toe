import type { GameMode, GameState, Player, Scoreboard } from './types';

const BASE = '/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(BASE + path, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });
  if (!res.ok) {
    let detail = `Request failed (${res.status})`;
    try {
      detail = (await res.json()).detail ?? detail;
    } catch {
      /* non-JSON error body */
    }
    throw new Error(detail);
  }
  return res.json() as Promise<T>;
}

export const api = {
  createGame: (mode: GameMode) =>
    request<GameState>('/games', { method: 'POST', body: JSON.stringify({ mode }) }),
  getGame: (id: string) => request<GameState>(`/games/${id}`),
  move: (id: string, player: Player, index: number) =>
    request<GameState>(`/games/${id}/moves`, { method: 'POST', body: JSON.stringify({ player, index }) }),
  undo: (id: string) => request<GameState>(`/games/${id}/undo`, { method: 'POST' }),
  reset: (id: string) => request<GameState>(`/games/${id}/reset`, { method: 'POST' }),
  getScoreboard: () => request<Scoreboard>('/scoreboard'),
  resetScoreboard: () => request<Scoreboard>('/scoreboard/reset', { method: 'POST' }),
};
