namespace TicTacToe.Api.Domain;

public static class Rules
{
    public static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // rows
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // columns
        [0, 4, 8], [2, 4, 6],            // diagonals
    ];

    public static (Player Winner, int[] Cells)? FindWinner(Player?[] board)
    {
        foreach (var line in Lines)
        {
            var p = board[line[0]];
            if (p is not null && board[line[1]] == p && board[line[2]] == p)
                return (p.Value, line);
        }
        return null;
    }
}
