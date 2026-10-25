# Tic Tac Toe — React + .NET

## 1. Project overview
A browser Tic Tac Toe game with a **React** frontend and an **ASP.NET Core (.NET 9)** REST backend, in one monorepo.
The backend owns all game state (board, turns, move history, status, scoreboard); the frontend only renders what the API returns.
Two modes: **Two Player** and **Play Against Computer** (human = X, computer = O).

```
backend/   TicTacToe.Api (domain + REST API), TicTacToe.Tests (xUnit)
frontend/  React + Vite + TypeScript, Vitest tests
```

## 2. Tech stack
- Frontend: React 18, TypeScript, Vite, Vitest + Testing Library
- Backend: .NET 9 minimal APIs, xUnit
- API style: REST/JSON. Storage: in-memory (singleton service, lock-protected)
- Source control: GitHub

## 3. Features implemented
3×3 board with locked cells · turn display and alternation · row/column/diagonal win detection with highlighted cells · draw detection · Reset Game · move history (number, player, row/column) · Undo Last Move (mode-aware) · session scoreboard with separate Reset Scoreboard · computer opponent (win → block → center → corner → any) · backend validation of out-of-board, occupied, after-completion and wrong-player moves.

## 4. Run the backend
Requires the .NET 9 SDK.
```bash
cd backend
dotnet run --project TicTacToe.Api      # http://localhost:5080
```

## 5. Run the frontend
Requires Node 18+.
```bash
cd frontend
npm install
npm run dev                             # http://localhost:5173
```
Start the backend first. Vite proxies `/api` to `http://localhost:5080` (CORS for :5173 is also enabled).

## 6. API summary
All responses are JSON; enums are strings. Errors use RFC 7807 problem details (`detail`, plus `code`).

| Method | Endpoint | Body | Purpose |
|---|---|---|---|
| POST | `/api/games` | `{ "mode": "TwoPlayer" \| "Computer" }` | Create game (201) |
| GET | `/api/games/{id}` | – | Get game state |
| POST | `/api/games/{id}/moves` | `{ "player": "X"\|"O", "index": 0-8 }` | Submit move (computer replies in the same response) |
| POST | `/api/games/{id}/undo` | – | Undo last move (mode-aware) |
| POST | `/api/games/{id}/reset` | – | Reset the game, scoreboard untouched |
| GET | `/api/scoreboard` | – | `{ xWins, oWins, draws }` |
| POST | `/api/scoreboard/reset` | – | Zero the scoreboard |

Cell index is 0–8, row-major (`index = row*3 + col`, 0-based). History shows 1-based "Row r, Column c".

**Game state response:** `gameId, board[9] ("X"|"O"|null), currentPlayer, mode, status (InProgress|Won|Draw), winner, winningCells[], moves[{moveNumber, player, index, row, column}], canUndo, scoreboard`.

**Errors:** `400 OutOfBounds`; `409 CellOccupied | GameCompleted | WrongPlayer | NothingToUndo | UndoNotAllowed`; `404` unknown game. A rejected move never changes state or turn.

## 7. Run tests
```bash
cd backend && dotnet test               # 27 tests: rules, undo, computer, scoreboard
cd frontend && npm test                 # 5 tests: rendering + API integration (mocked fetch)
```

## 8. AI tools and prompt summary
Built with Claude Code (Claude Sonnet 5.5). Workflow:
1. Gave the assignment PDF and asked the AI to read it and raise doubts before coding.
2. Resolved the doubts: React over Angular (spec contradicts itself), in-memory storage, Undo Option A, monorepo.
3. AI generated the domain model, REST endpoints, xUnit tests, React UI and this README; it installed the .NET SDK locally.
4. Verified by running all tests, the build, and a live curl smoke test through the Vite proxy.

Review these carefully before the panel: `Game.cs` (state derivation, undo), `GameService.cs` (score-once), `ComputerPlayer.cs`.

## 9. Design decisions
- **State is derived from the move list.** Board, current player, status, winner and winning cells are computed from `moves`, so Undo/Reset can't leave inconsistent state.
- **Computer replies inside the move request**, so the UI gets one consistent state and a user can't act between the two moves.
- **Scoreboard scored once** via a per-game `scored` flag in `GameService`; moves after completion are rejected anyway.
- Domain logic is plain C# classes (no HTTP), so it is unit-testable without a server.
- Frontend holds no game rules; it sends the move for `currentPlayer` and renders the response.

## 10. Clarifications and assumptions
- **Undo choice: Option A** — Undo is disabled once a game is won or drawn; the scoreboard stays final.
- Computer mode undo removes the computer move and the human move before it. In Computer mode an in-progress game always has an even number of moves, so this pair always exists.
- Changing mode creates a new game; the scoreboard (session-wide, on the backend) is kept.
- Reset Game keeps the same game id and mode. The game id is kept in `sessionStorage` so a page refresh resumes the game.
- The frontend is React, as listed in the Technology Expectations (the intro text says Angular).

## 11. Known limitations
- In-memory only: restarting the API loses games and scores. The scoreboard is global to the server, not per user.
- Finished games are never evicted from memory.
- No authentication; no frontend end-to-end tests.

## 12. Future improvements
Option B (undo after completion with scoreboard rollback), SQLite persistence, per-session scoreboards, smarter (minimax) computer difficulty levels, API integration tests with `WebApplicationFactory`, Docker compose.
