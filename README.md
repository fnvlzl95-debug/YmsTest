# YMS Equipment Reservation Practice Project

## Run
1. `npm install` (root)
2. `npm run dev`

- API: `http://localhost:5000/swagger`
- Client: `http://localhost:3000`

## Oracle Run
- `npm run dev:oracle`
- Requires Oracle Listener running (`localhost:1521/XEPDB1`)

## P00090-Style Structure
- Client (`client/src`)
  - `Main.jsx`
  - `Enums.js`
  - `OpenLapResv.jsx`
  - `OpenLapResvEdit.jsx`
  - `OpenLapEqp.jsx`
  - `OpenLapAuth.jsx`
- Server (`server`)
  - `Main.cs`
  - `Enums.cs`
  - `Mailer.cs`

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
