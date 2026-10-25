namespace TicTacToe.Api.Domain;

public enum Player { X, O }

public enum GameMode { TwoPlayer, Computer }

public enum GameStatus { InProgress, Won, Draw }

public record Move(int MoveNumber, Player Player, int Index)
{
    public int Row => Index / 3 + 1;
    public int Column => Index % 3 + 1;
}

public record Scoreboard(int XWins, int OWins, int Draws);

public enum GameErrorCode { OutOfBounds, CellOccupied, GameCompleted, WrongPlayer, NothingToUndo, UndoNotAllowed }

public class GameException(GameErrorCode code, string message) : Exception(message)
{
    public GameErrorCode Code { get; } = code;
}
