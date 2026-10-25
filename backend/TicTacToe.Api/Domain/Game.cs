namespace TicTacToe.Api.Domain;

/// <summary>
/// A single game. Board, status and winner are always derived by replaying the move list,
/// so undo can never leave the state inconsistent.
/// </summary>
public class Game(Guid id, GameMode mode)
{
    private readonly List<Move> _moves = [];

    public Guid Id { get; } = id;
    public GameMode Mode { get; } = mode;
    public IReadOnlyList<Move> Moves => _moves;

    public Player?[] Board
    {
        get
        {
            var board = new Player?[9];
            foreach (var m in _moves) board[m.Index] = m.Player;
            return board;
        }
    }

    public Player CurrentPlayer => _moves.Count % 2 == 0 ? Player.X : Player.O;

    public (Player Winner, int[] Cells)? Result => Rules.FindWinner(Board);

    public GameStatus Status =>
        Result is not null ? GameStatus.Won
        : _moves.Count == 9 ? GameStatus.Draw
        : GameStatus.InProgress;

    public Player? Winner => Result?.Winner;
    public int[] WinningCells => Result?.Cells ?? [];

    /// <summary>Validates and applies a move. In Computer mode the computer replies automatically.</summary>
    public void Play(Player player, int index)
    {
        Apply(player, index);

        if (Mode == GameMode.Computer && Status == GameStatus.InProgress)
            Apply(Player.O, ComputerPlayer.ChooseMove(Board));
    }

    private void Apply(Player player, int index)
    {
        if (index is < 0 or > 8)
            throw new GameException(GameErrorCode.OutOfBounds, "Cell index must be between 0 and 8.");
        if (Status != GameStatus.InProgress)
            throw new GameException(GameErrorCode.GameCompleted, "The game is already completed.");
        if (player != CurrentPlayer)
            throw new GameException(GameErrorCode.WrongPlayer, $"It is {CurrentPlayer}'s turn.");
        if (Board[index] is not null)
            throw new GameException(GameErrorCode.CellOccupied, "That cell is already taken.");

        _moves.Add(new Move(_moves.Count + 1, player, index));
    }

    public bool CanUndo => Status == GameStatus.InProgress && _moves.Count > 0;

    /// <summary>
    /// Option A: undo is disabled once a game is completed, so the scoreboard stays final.
    /// Two Player removes one move; Computer removes the computer move and the human move before it.
    /// </summary>
    public void Undo()
    {
        if (_moves.Count == 0)
            throw new GameException(GameErrorCode.NothingToUndo, "There are no moves to undo.");
        if (Status != GameStatus.InProgress)
            throw new GameException(GameErrorCode.UndoNotAllowed, "Undo is disabled after a game is completed.");

        var count = Mode == GameMode.Computer ? Math.Min(2, _moves.Count) : 1;
        _moves.RemoveRange(_moves.Count - count, count);
    }

    public void Reset() => _moves.Clear();
}
