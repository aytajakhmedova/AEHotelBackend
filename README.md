# AEHotel Backend

ASP.NET Core Web API for the AE Hotel reservation frontend. This repository is independent from the React frontend repository.

## Open in Visual Studio 2022

1. Open `AEHotel.sln` with Visual Studio Professional 2022.
2. Set `AEHotel.API` as the startup project.
3. Select the `http` launch profile and run the project.

The API listens at `http://localhost:5080`. Swagger UI is available at `http://localhost:5080/swagger` while the environment is Development.

## Projects

| Project | Responsibility |
| --- | --- |
| `AEHotel.API` | Controllers, CORS, Swagger, and dependency injection composition |
| `AEHotel.Domain` | `Common/BaseEntity`, `Entities`, and `Configurations` |
| `AEHotel.Repository` | `Data/AppDbContext`, `Migrations`, and `Repositories` with `Interfaces` |
| `AEHotel.Service` | `DTOs`, `Exceptions`, `Helpers`, and `Services` with `Interfaces` |

Reference direction:

`AEHotel.API` -> `AEHotel.Service` -> `AEHotel.Repository` -> `AEHotel.Domain`

## Database

The development connection string uses SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=AEHotelDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

It is stored in `AEHotel.API/appsettings.json` under `ConnectionStrings:DefaultConnection`. For SQL Server Express, replace the server name with `localhost\SQLEXPRESS`.

Create the database from the repository root:

```powershell
dotnet tool restore
dotnet ef database update --project AEHotel.Repository --startup-project AEHotel.API
```

The initial migration creates `AEHotelDb` and seeds one hotel with two rooms.

## React frontend

The React app runs separately, by default at `http://localhost:5173`. CORS allows:

- `http://localhost:5173`
- `http://127.0.0.1:5173`
- `http://localhost:4173`
- `http://localhost:3000`

Use `http://localhost:5080` as the API base URL. Copy `docs/react-fetch-examples.js` into the frontend project when you are ready to replace the local mock data. Do not copy frontend files into this repository.

Example:

```javascript
const response = await fetch("http://localhost:5080/api/hotels");
const hotels = await response.json();
```

Available routes:

| Method | Route |
| --- | --- |
| GET | `/api/health` |
| GET, POST | `/api/hotels` |
| GET, PUT, DELETE | `/api/hotels/{id}` |
| GET | `/api/hotels/{hotelId}/rooms` |
| GET, POST | `/api/rooms` |
| GET, PUT, DELETE | `/api/rooms/{id}` |
| GET, POST | `/api/reservations` |
| GET, PUT, DELETE | `/api/reservations/{id}` |

JSON uses camelCase property names. A failed request returns `{ "message": "..." }`.
