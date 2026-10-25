using TicTacToe.Api.Domain;
using static TicTacToe.Api.Domain.Player;

namespace TicTacToe.Tests;

public class GameTests
{
    private static Game NewGame(GameMode mode = GameMode.TwoPlayer) => new(Guid.NewGuid(), mode);

    private static void PlaySequence(Game g, params int[] cells)
    {
        foreach (var c in cells) g.Play(g.CurrentPlayer, c);
    }

    [Fact]
    public void ValidMove_PlacesMarkAndRecordsHistory()
    {
        var g = NewGame();
        g.Play(X, 0);
        Assert.Equal(X, g.Board[0]);
        var m = Assert.Single(g.Moves);
        Assert.Equal((1, X, 1, 1), (m.MoveNumber, m.Player, m.Row, m.Column));
    }

    [Fact]
    public void Turns_AlternateAfterEveryValidMove()
    {
        var g = NewGame();
        Assert.Equal(X, g.CurrentPlayer);
        g.Play(X, 0);
        Assert.Equal(O, g.CurrentPlayer);
        g.Play(O, 1);
        Assert.Equal(X, g.CurrentPlayer);
    }

    [Theory]
    [InlineData(-1, GameErrorCode.OutOfBounds)]
    [InlineData(9, GameErrorCode.OutOfBounds)]
    public void InvalidMove_OutsideBoard_Rejected(int index, GameErrorCode code)
    {
        var g = NewGame();
        Assert.Equal(code, Assert.Throws<GameException>(() => g.Play(X, index)).Code);
        Assert.Equal(X, g.CurrentPlayer);
    }

    [Fact]
    public void InvalidMove_OccupiedCell_RejectedAndTurnUnchanged()
    {
        var g = NewGame();
        g.Play(X, 4);
        var ex = Assert.Throws<GameException>(() => g.Play(O, 4));
        Assert.Equal(GameErrorCode.CellOccupied, ex.Code);
        Assert.Equal(O, g.CurrentPlayer);
        Assert.Single(g.Moves);
    }

    [Fact]
    public void InvalidMove_WrongPlayer_Rejected()
    {
        var g = NewGame();
        var ex = Assert.Throws<GameException>(() => g.Play(O, 0));
        Assert.Equal(GameErrorCode.WrongPlayer, ex.Code);
        Assert.Empty(g.Moves);
    }

    [Fact]
    public void RowWin_DetectedWithWinningCells()
    {
        var g = NewGame();
        PlaySequence(g, 0, 3, 1, 4, 2);
        Assert.Equal(GameStatus.Won, g.Status);
        Assert.Equal(X, g.Winner);
        Assert.Equal([0, 1, 2], g.WinningCells);
    }

    [Fact]
    public void ColumnWin_Detected()
    {
        var g = NewGame();
        PlaySequence(g, 0, 1, 3, 2, 6);
        Assert.Equal(GameStatus.Won, g.Status);
        Assert.Equal([0, 3, 6], g.WinningCells);
    }

    [Fact]
    public void DiagonalWin_Detected()
    {
        var g = NewGame();
        PlaySequence(g, 0, 1, 4, 2, 8);
        Assert.Equal(GameStatus.Won, g.Status);
        Assert.Equal([0, 4, 8], g.WinningCells);

        var anti = NewGame();
        PlaySequence(anti, 2, 0, 4, 1, 6);
        Assert.Equal([2, 4, 6], anti.WinningCells);
    }

    [Fact]
    public void Draw_Detected_WhenBoardFullWithoutWinner()
    {
        var g = NewGame();
        PlaySequence(g, 0, 1, 2, 4, 3, 5, 7, 6, 8);
        Assert.Equal(GameStatus.Draw, g.Status);
        Assert.Null(g.Winner);
        Assert.Empty(g.WinningCells);
    }

    [Fact]
    public void MoveAfterCompletion_Rejected()
    {
        var g = NewGame();
        PlaySequence(g, 0, 3, 1, 4, 2);
        var ex = Assert.Throws<GameException>(() => g.Play(O, 8));
        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
        Assert.Equal(5, g.Moves.Count);
    }

    [Fact]
    public void Reset_ClearsBoardHistoryStatusAndReturnsToX()
    {
        var g = NewGame();
        PlaySequence(g, 0, 3, 1, 4, 2);
        g.Reset();
        Assert.All(g.Board, c => Assert.Null(c));
        Assert.Empty(g.Moves);
        Assert.Equal(GameStatus.InProgress, g.Status);
        Assert.Equal(X, g.CurrentPlayer);
    }

    [Fact]
    public void Undo_TwoPlayer_RemovesOnlyLastMove()
    {
        var g = NewGame();
        g.Play(X, 0);
        g.Play(O, 1);
        g.Undo();
        Assert.Single(g.Moves);
        Assert.Null(g.Board[1]);
        Assert.Equal(O, g.CurrentPlayer);
    }

    [Fact]
    public void Undo_ComputerMode_RemovesComputerAndHumanMove()
    {
        var g = NewGame(GameMode.Computer);
        g.Play(X, 0);
        Assert.Equal(2, g.Moves.Count);
        g.Undo();
        Assert.Empty(g.Moves);
        Assert.Equal(X, g.CurrentPlayer);
        Assert.All(g.Board, c => Assert.Null(c));
    }

    [Fact]
    public void Undo_WithNoMoves_Throws_AndCanUndoIsFalse()
    {
        var g = NewGame();
        Assert.False(g.CanUndo);
        Assert.Equal(GameErrorCode.NothingToUndo, Assert.Throws<GameException>(g.Undo).Code);
    }

    [Fact]
    public void Undo_AfterCompletion_Disabled()
    {
        var g = NewGame();
        PlaySequence(g, 0, 3, 1, 4, 2);
        Assert.False(g.CanUndo);
        Assert.Equal(GameErrorCode.UndoNotAllowed, Assert.Throws<GameException>(g.Undo).Code);
        Assert.Equal(GameStatus.Won, g.Status);
    }

    [Fact]
    public void ComputerMode_RepliesAutomatically_AsO()
    {
        var g = NewGame(GameMode.Computer);
        g.Play(X, 0);
        Assert.Equal(2, g.Moves.Count);
        Assert.Equal(O, g.Moves[1].Player);
        Assert.Equal(X, g.CurrentPlayer);
    }

    [Fact]
    public void ComputerMode_DoesNotMoveAfterHumanWins()
    {
        var g = NewGame(GameMode.Computer);
        // Human tries to win; computer replies in between. Drive until game ends.
        var order = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        foreach (var i in order)
        {
            if (g.Status != GameStatus.InProgress) break;
            if (g.Board[i] is null) g.Play(X, i);
        }
        Assert.NotEqual(GameStatus.InProgress, g.Status);
        var count = g.Moves.Count;
        Assert.Throws<GameException>(() => g.Play(X, 0));
        Assert.Equal(count, g.Moves.Count);
        Assert.True(g.Moves.Select(m => m.Index).Distinct().Count() == count);
    }
}

public class ComputerPlayerTests
{
    private static Player?[] Board(string s) =>
        s.Select(c => c switch { 'X' => (Player?)X, 'O' => O, _ => null }).ToArray();

    [Fact]
    public void WinsWhenPossible_BeforeBlocking()
    {
        // O can win at 2; X threatens 6.
        Assert.Equal(2, ComputerPlayer.ChooseMove(Board("OO.XX.X..")));
    }

    [Fact]
    public void BlocksX_WhenXCanWin()
    {
        Assert.Equal(2, ComputerPlayer.ChooseMove(Board("XX..O....")));
    }

    [Fact]
    public void TakesCenter_WhenAvailable()
    {
        Assert.Equal(4, ComputerPlayer.ChooseMove(Board("X........")));
    }

    [Fact]
    public void TakesCorner_WhenCenterTaken()
    {
        Assert.Equal(0, ComputerPlayer.ChooseMove(Board("....X....")));
    }

    [Fact]
    public void TakesAnyCell_WhenNoCornerOrCenterAvailable()
    {
        // Center and all corners are taken; only edges 1,3,5,7 remain.
        var board = Board("XOXOXOOXO");
        board[1] = board[3] = board[5] = board[7] = null;
        Assert.Contains(ComputerPlayer.ChooseMove(board), new[] { 1, 3, 5, 7 });
    }
}

public class GameServiceTests
{
    private static void Win(GameService s, Guid id) // X wins top row
    {
        foreach (var (p, i) in new[] { (X, 0), (O, 3), (X, 1), (O, 4), (X, 2) }) s.Play(id, p, i);
    }

    [Fact]
    public void Scoreboard_UpdatesOnce_OnWin()
    {
        var s = new GameService();
        var g = s.Create(GameMode.TwoPlayer);
        Win(s, g.Id);
        Assert.Equal(new Scoreboard(1, 0, 0), s.Scoreboard);
        Assert.Throws<GameException>(() => s.Play(g.Id, O, 8));
        Assert.Equal(new Scoreboard(1, 0, 0), s.Scoreboard);
    }

    [Fact]
    public void Scoreboard_UpdatesOnDraw()
    {
        var s = new GameService();
        var g = s.Create(GameMode.TwoPlayer);
        var cells = new[] { 0, 1, 2, 4, 3, 5, 7, 6, 8 };
        for (var i = 0; i < cells.Length; i++) s.Play(g.Id, i % 2 == 0 ? X : O, cells[i]);
        Assert.Equal(new Scoreboard(0, 0, 1), s.Scoreboard);
    }

    [Fact]
    public void ResetGame_KeepsScoreboard_ResetScoreboardClearsIt()
    {
        var s = new GameService();
        var g = s.Create(GameMode.TwoPlayer);
        Win(s, g.Id);
        s.Reset(g.Id);
        Assert.Equal(new Scoreboard(1, 0, 0), s.Scoreboard);
        Win(s, g.Id); // same session can be scored again after reset
        Assert.Equal(new Scoreboard(2, 0, 0), s.Scoreboard);
        s.ResetScoreboard();
        Assert.Equal(new Scoreboard(0, 0, 0), s.Scoreboard);
    }

    [Fact]
    public void UnknownGame_ReturnsNull()
    {
        var s = new GameService();
        Assert.Null(s.Get(Guid.NewGuid()));
        Assert.Null(s.Play(Guid.NewGuid(), X, 0));
    }
}
