import { useCallback, useEffect, useState } from 'react';
import { api } from './api';
import { Board, MoveHistory, ScoreboardPanel, statusMessage } from './components';
import type { GameMode, GameState } from './types';

const STORAGE_KEY = 'tictactoe.gameId';

export default function App() {
  const [game, setGame] = useState<GameState | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Runs an API call, renders the state the backend returns, surfaces errors.
  const run = useCallback(async (call: () => Promise<GameState>) => {
    setBusy(true);
    try {
      const next = await call();
      setGame(next);
      setError(null);
      try { sessionStorage.setItem(STORAGE_KEY, next.gameId); } catch { /* storage unavailable */ }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Something went wrong');
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    let saved: string | null = null;
    try { saved = sessionStorage.getItem(STORAGE_KEY); } catch { /* storage unavailable */ }
    run(async () => {
      if (saved) {
        try { return await api.getGame(saved); } catch { /* game gone (e.g. API restarted) */ }
      }
      return api.createGame('TwoPlayer');
    });
  }, [run]);

  if (!game) {
    return <main className="app"><h1>Tic Tac Toe</h1><p className={error ? 'error' : 'muted'}>{error ?? 'Loading…'}</p></main>;
  }

  const switchMode = (mode: GameMode) => {
    if (mode !== game.mode) run(() => api.createGame(mode));
  };

  const resetScoreboard = async () => {
    // The scoreboard lives on the backend; re-fetch the game so every panel shows the same state.
    await api.resetScoreboard().catch((e) => setError(e.message));
    run(() => api.getGame(game.gameId));
  };

  return (
    <main className="app">
      <h1>Tic Tac Toe</h1>

      <div className="modes" role="radiogroup" aria-label="Game mode">
        {([['TwoPlayer', 'Two Player'], ['Computer', 'Play Against Computer']] as const).map(([mode, label]) => (
          <button
            key={mode}
            role="radio"
            aria-checked={game.mode === mode}
            className={game.mode === mode ? 'chip active' : 'chip'}
            disabled={busy}
            onClick={() => switchMode(mode)}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="layout">
        <section className="panel main">
          <p className={`status ${game.status !== 'InProgress' ? 'done' : ''}`} role="status">{statusMessage(game)}</p>
          <p className="muted">
            Mode: {game.mode === 'Computer' ? 'Play Against Computer (you are X)' : 'Two Player'}
          </p>
          <Board game={game} disabled={busy} onPlay={(i) => run(() => api.move(game.gameId, game.currentPlayer, i))} />
          <div className="actions">
            <button disabled={busy || !game.canUndo} onClick={() => run(() => api.undo(game.gameId))}>Undo Last Move</button>
            <button onClick={() => run(() => api.reset(game.gameId))} disabled={busy}>Reset Game</button>
          </div>
          {error && <p className="error" role="alert">{error}</p>}
        </section>

        <div className="side">
          <ScoreboardPanel score={game.scoreboard} onReset={resetScoreboard} />
          <MoveHistory moves={game.moves} />
        </div>
      </div>
    </main>
  );
}
