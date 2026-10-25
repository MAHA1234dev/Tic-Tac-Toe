namespace TicTacToe.Api.Domain;

/// <summary>Deterministic computer strategy: win, block, center, corner, any.</summary>
public static class ComputerPlayer
{
    private static readonly int[] Corners = [0, 2, 6, 8];

    public static int ChooseMove(Player?[] board, Player me = Player.O)
    {
        var opponent = me == Player.O ? Player.X : Player.O;

        var win = FindWinningCell(board, me);
        if (win is not null) return win.Value;

        var block = FindWinningCell(board, opponent);
        if (block is not null) return block.Value;

        if (board[4] is null) return 4;

        foreach (var c in Corners)
            if (board[c] is null) return c;

        for (var i = 0; i < 9; i++)
            if (board[i] is null) return i;

        throw new InvalidOperationException("No available cells.");
    }

    private static int? FindWinningCell(Player?[] board, Player player)
    {
        for (var i = 0; i < 9; i++)
        {
            if (board[i] is not null) continue;
            board[i] = player;
            var wins = Rules.FindWinner(board) is not null;
            board[i] = null;
            if (wins) return i;
        }
        return null;
    }
}
