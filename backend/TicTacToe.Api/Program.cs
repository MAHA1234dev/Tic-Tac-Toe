using System.Text.Json.Serialization;
using TicTacToe.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameService>();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

GameStateResponse ToResponse(Game g, GameService svc) => new(
    g.Id,
    g.Board.Select(p => p?.ToString()).ToArray(),
    g.CurrentPlayer,
    g.Mode,
    g.Status,
    g.Winner,
    g.WinningCells,
    g.Moves.Select(m => new MoveDto(m.MoveNumber, m.Player, m.Index, m.Row, m.Column)).ToArray(),
    g.CanUndo,
    svc.Scoreboard);

IResult NotFound(Guid id) => Results.Problem($"Game {id} not found.", statusCode: 404);

IResult Run(Func<Game?> action, Guid id, GameService svc)
{
    try
    {
        var game = action();
        return game is null ? NotFound(id) : Results.Ok(ToResponse(game, svc));
    }
    catch (GameException ex)
    {
        var status = ex.Code == GameErrorCode.OutOfBounds ? 400 : 409;
        return Results.Problem(ex.Message, statusCode: status, extensions: new Dictionary<string, object?> { ["code"] = ex.Code.ToString() });
    }
}

var api = app.MapGroup("/api");

api.MapPost("/games", (CreateGameRequest? req, GameService svc) =>
{
    var game = svc.Create(req?.Mode ?? GameMode.TwoPlayer);
    return Results.Created($"/api/games/{game.Id}", ToResponse(game, svc));
});

api.MapGet("/games/{id:guid}", (Guid id, GameService svc) =>
    Run(() => svc.Get(id), id, svc));

api.MapPost("/games/{id:guid}/moves", (Guid id, MoveRequest req, GameService svc) =>
    Run(() => svc.Play(id, req.Player, req.Index), id, svc));

api.MapPost("/games/{id:guid}/undo", (Guid id, GameService svc) =>
    Run(() => svc.Undo(id), id, svc));

api.MapPost("/games/{id:guid}/reset", (Guid id, GameService svc) =>
    Run(() => svc.Reset(id), id, svc));

api.MapGet("/scoreboard", (GameService svc) => svc.Scoreboard);
api.MapPost("/scoreboard/reset", (GameService svc) => svc.ResetScoreboard());

app.Run();

public record CreateGameRequest(GameMode Mode);
public record MoveRequest(Player Player, int Index);
public record MoveDto(int MoveNumber, Player Player, int Index, int Row, int Column);
public record GameStateResponse(
    Guid GameId, string?[] Board, Player CurrentPlayer, GameMode Mode, GameStatus Status,
    Player? Winner, int[] WinningCells, MoveDto[] Moves, bool CanUndo, Scoreboard Scoreboard);

public partial class Program;
