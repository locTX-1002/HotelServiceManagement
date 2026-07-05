# HSMS Backend

ASP.NET Core Web API — target `net8.0`, build được với SDK 8/9/10.

```bash
dotnet restore
cd HotelServiceManagement.Api
dotnet run    # http://localhost:5000 | Swagger: /swagger | Health: /health
```

App tự migrate + seed khi chạy Development. Hướng dẫn đầy đủ: [README gốc](../README.md).

| Project | Vai trò |
|---|---|
| `HotelServiceManagement.Api` | Controllers, Program.cs, Swagger, CORS |
| `HotelServiceManagement.Application` | DTOs, Interfaces, Services, Validators |
| `HotelServiceManagement.Domain` | 13 entity + enums (schema Report 2 §4.4) |
| `HotelServiceManagement.Infrastructure` | AppDbContext, Migrations, DbSeeder |

Migration (chỉ Lộc tạo — xem CONTRIBUTING.md):

```bash
dotnet tool restore
dotnet ef migrations add <Ten> -p HotelServiceManagement.Infrastructure -s HotelServiceManagement.Api
dotnet ef database update -p HotelServiceManagement.Infrastructure -s HotelServiceManagement.Api
```
