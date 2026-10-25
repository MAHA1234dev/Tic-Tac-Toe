namespace TicTacToe.Api.Domain;

/// <summary>In-memory store for games and the session-level scoreboard. Thread-safe.</summary>
public class GameService
{
    private readonly Dictionary<Guid, Game> _games = [];
    private readonly Dictionary<Guid, bool> _scored = [];
    private readonly object _lock = new();
    private int _x, _o, _draws;

    public Scoreboard Scoreboard
    {
        get { lock (_lock) return new Scoreboard(_x, _o, _draws); }
    }

    public Game Create(GameMode mode)
    {
        lock (_lock)
        {
            var game = new Game(Guid.NewGuid(), mode);
            _games[game.Id] = game;
            return game;
        }
    }

    public Game? Get(Guid id)
    {
        lock (_lock) return _games.GetValueOrDefault(id);
    }

    public Game? Play(Guid id, Player player, int index)
    {
        lock (_lock)
        {
            var game = Get(id);
            if (game is null) return null;
            game.Play(player, index);
            RecordIfCompleted(game);
            return game;
        }
    }

    public Game? Undo(Guid id)
    {
        lock (_lock)
        {
            var game = Get(id);
            game?.Undo();
            return game;
        }
    }

    public Game? Reset(Guid id)
    {
        lock (_lock)
        {
            var game = Get(id);
            if (game is null) return null;
            game.Reset();
            _scored.Remove(id);
            return game;
        }
    }

    public Scoreboard ResetScoreboard()
    {
        lock (_lock)
        {
            _x = _o = _draws = 0;
            return Scoreboard;
        }
    }

    // A game is scored at most once; Play rejects moves after completion so this is also guarded by state.
    private void RecordIfCompleted(Game game)
    {
        if (game.Status == GameStatus.InProgress || _scored.GetValueOrDefault(game.Id)) return;
        _scored[game.Id] = true;
        if (game.Status == GameStatus.Draw) _draws++;
        else if (game.Winner == Player.X) _x++;
        else _o++;
    }
}
