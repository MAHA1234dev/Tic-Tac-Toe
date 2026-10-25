import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from './App';
import type { GameState } from './types';

const emptyGame = (over: Partial<GameState> = {}): GameState => ({
  gameId: 'g1', board: Array(9).fill(null), currentPlayer: 'X', mode: 'TwoPlayer', status: 'InProgress',
  winner: null, winningCells: [], moves: [], canUndo: false, scoreboard: { xWins: 0, oWins: 0, draws: 0 }, ...over,
});

function mockFetch(handler: (url: string, init?: RequestInit) => GameState | object) {
  const fn = vi.fn(async (url: string, init?: RequestInit) => ({ ok: true, status: 200, json: async () => handler(url, init) }));
  vi.stubGlobal('fetch', fn);
  return fn;
}

beforeEach(() => sessionStorage.clear());
afterEach(() => vi.unstubAllGlobals());

describe('App', () => {
  it('creates a game on load and renders turn, mode and empty history', async () => {
    mockFetch(() => emptyGame());
    render(<App />);
    expect(await screen.findByText("Player X's turn")).toBeInTheDocument();
    expect(screen.getByText(/Mode: Two Player/)).toBeInTheDocument();
    expect(screen.getByText('No moves yet.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Undo Last Move' })).toBeDisabled();
  });

  it('submits a move to the backend and renders the returned state', async () => {
    const fetchFn = mockFetch((url, init) =>
      url.endsWith('/moves')
        ? emptyGame({
            board: ['X', ...Array(8).fill(null)], currentPlayer: 'O', canUndo: true,
            moves: [{ moveNumber: 1, player: 'X', index: 0, row: 1, column: 1 }],
          })
        : emptyGame());
    render(<App />);
    await userEvent.click(await screen.findByRole('button', { name: 'Row 1, Column 1, empty' }));
    expect(await screen.findByText("Player O's turn")).toBeInTheDocument();
    expect(screen.getByText('Row 1, Column 1')).toBeInTheDocument();
    const moveCall = fetchFn.mock.calls.find(([u]) => u.endsWith('/moves'))!;
    expect(JSON.parse(moveCall[1]!.body as string)).toEqual({ player: 'X', index: 0 });
  });

  it('shows winner, highlights winning cells and locks the board', async () => {
    mockFetch(() => emptyGame({
      board: ['X', 'X', 'X', 'O', 'O', null, null, null, null], status: 'Won', winner: 'X',
      winningCells: [0, 1, 2], scoreboard: { xWins: 1, oWins: 0, draws: 0 },
    }));
    render(<App />);
    expect(await screen.findByText('Player X wins!')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Row 1, Column 1, X/ })).toHaveClass('win');
    expect(screen.getByRole('button', { name: /Row 3, Column 3, empty/ })).toBeDisabled();
    expect(screen.getByTestId('score-X Wins')).toHaveTextContent('1');
  });

  it('shows a draw message', async () => {
    mockFetch(() => emptyGame({ status: 'Draw', board: Array(9).fill('X') }));
    render(<App />);
    expect(await screen.findByText("It's a draw!")).toBeInTheDocument();
  });

  it('shows backend error message for rejected requests', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => ({ ok: false, status: 409, json: async () => ({ detail: 'That cell is already taken.' }) })));
    render(<App />);
    await waitFor(() => expect(screen.getByText('That cell is already taken.')).toBeInTheDocument());
  });
});
