# YMS Equipment Reservation Practice Project

## Run
1. `npm install` (root)
2. `npm run dev`

- API: `http://localhost:5000/swagger`
- Client: `http://localhost:3000`

## Oracle Run
- `npm run dev:oracle`
- Requires Oracle Listener running (`localhost:1521/XEPDB1`)

## Build Check
- `dotnet build server/YMS.Server.csproj`
- `npm --prefix client run lint`
- `npm --prefix client run build`

## Debug (VS Code)
- Run config: `YMS Full Stack`

## Notes
- DB provider switch key: `DB_PROVIDER` (`Oracle` or `Sqlite`)
- Connections are configured in `server/appsettings*.json`
- If `dotnet` is not recognized, restart terminal after PATH update.
