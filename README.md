# AEHotel Backend

ASP.NET Core Web API for the AEHotel frontend. Hotel, flight, and tour are separate services with their own search and booking flows. All three share the same user account and `GET /api/bookings/mine` ("Rezervasiyalarım").

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

## Product model

The homepage has three independent sections. Completing a hotel stay with a flight or tour is optional.

| Section | Search fields | User flow |
| --- | --- | --- |
| Hotel | `city`, `checkIn`, `checkOut`, `guests` | Search hotels, pick a room, book |
| Flight | `origin`, `destination`, `date`, `passengers` | Search flights, compare (`sort=price`), book |
| Tour | `destination`, `date`, `guests` | View program and price, book |

After a hotel booking the API can return optional trip suggestions (`tripSuggestions` on the booking, or `GET /api/trip-suggestions`). The user can ignore them and keep only the hotel reservation.

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

Startup also migrates and seeds hotels, flights, and tours.

## React frontend

The React app runs separately, by default at `http://localhost:5173`. CORS allows:

- `http://localhost:5173`
- `http://127.0.0.1:5173`
- `http://localhost:4173`
- `http://localhost:3000`

Use `http://localhost:5080` as the API base URL. Copy `docs/react-fetch-examples.js` into the frontend project when you are ready to replace the local mock data. Do not copy frontend files into this repository.

JSON uses camelCase property names. A failed request returns `{ "message": "..." }`. User bookings require `Authorization: Bearer {token}`.

### Search and catalog

| Method | Route |
| --- | --- |
| GET | `/api/health` |
| GET | `/api/hotels?city=&checkIn=&checkOut=&guests=` |
| GET, POST | `/api/hotels` |
| GET, PUT, DELETE | `/api/hotels/{id}` |
| GET | `/api/hotels/{hotelId}/rooms?checkIn=&checkOut=&guests=` |
| GET, POST | `/api/rooms` |
| GET, PUT, DELETE | `/api/rooms/{id}` |
| GET | `/api/rooms/{id}/quote?checkIn=&checkOut=&guests=` |
| GET | `/api/flights?origin=&destination=&date=&passengers=&sort=price` |
| GET, POST | `/api/flights` |
| GET, PUT, DELETE | `/api/flights/{id}` |
| GET | `/api/tours?destination=&date=&guests=` |
| GET, POST | `/api/tours` |
| GET, PUT, DELETE | `/api/tours/{id}` |
| GET | `/api/trip-suggestions?city=&checkIn=&checkOut=&guests=` |
| GET | `/api/site` |
| GET | `/api/site/cities` |
| POST | `/api/uploads` (admin) |

### Account and reservations

| Method | Route |
| --- | --- |
| POST | `/api/auth/register` |
| POST | `/api/auth/login` |
| GET | `/api/bookings/mine?bookingType=hotel\|flight\|tour` |
| GET, POST | `/api/bookings` |
| GET, PUT, DELETE | `/api/bookings/{id}` |
