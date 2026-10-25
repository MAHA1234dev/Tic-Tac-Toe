import type { GameState, MoveDto, Player, Scoreboard } from './types';

export function statusMessage(g: GameState): string {
  if (g.status === 'Won') return `Player ${g.winner} wins!`;
  if (g.status === 'Draw') return "It's a draw!";
  return `Player ${g.currentPlayer}'s turn`;
}

export function Board({ game, disabled, onPlay }: { game: GameState; disabled: boolean; onPlay: (i: number) => void }) {
  return (
    <div className="board" role="grid" aria-label="Tic Tac Toe board">
      {game.board.map((cell, i) => {
        const win = game.winningCells.includes(i);
        const locked = disabled || cell !== null || game.status !== 'InProgress';
        return (
          <button
            key={i}
            className={`cell ${cell ? `mark-${cell}` : ''} ${win ? 'win' : ''}`}
            aria-label={`Row ${Math.floor(i / 3) + 1}, Column ${(i % 3) + 1}${cell ? `, ${cell}` : ', empty'}`}
            disabled={locked}
            onClick={() => onPlay(i)}
          >
            {cell}
          </button>
        );
      })}
    </div>
  );
}

export function MoveHistory({ moves }: { moves: MoveDto[] }) {
  return (
    <section className="panel">
      <h2>Move History</h2>
      {moves.length === 0 ? (
        <p className="muted">No moves yet.</p>
      ) : (
        <table>
          <thead>
            <tr><th>Move</th><th>Player</th><th>Position</th></tr>
          </thead>
          <tbody>
            {moves.map((m) => (
              <tr key={m.moveNumber}>
                <td>{m.moveNumber}</td>
                <td>{m.player}</td>
                <td>Row {m.row}, Column {m.column}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

export function ScoreboardPanel({ score, onReset }: { score: Scoreboard; onReset: () => void }) {
  const items: [string, number][] = [['X Wins', score.xWins], ['O Wins', score.oWins], ['Draws', score.draws]];
  return (
    <section className="panel">
      <h2>Scoreboard</h2>
      <div className="scores">
        {items.map(([label, value]) => (
          <div key={label} className="score">
            <span className="score-value" data-testid={`score-${label}`}>{value}</span>
            <span className="muted">{label}</span>
          </div>
        ))}
      </div>
      <button className="secondary" onClick={onReset}>Reset Scoreboard</button>
    </section>
  );
}

export type { Player };
