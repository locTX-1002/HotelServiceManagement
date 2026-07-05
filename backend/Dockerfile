# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file project và restore dependencies (tận dụng docker layer cache)
COPY ["HotelServiceManagement.Api/HotelServiceManagement.Api.csproj", "HotelServiceManagement.Api/"]
COPY ["HotelServiceManagement.Application/HotelServiceManagement.Application.csproj", "HotelServiceManagement.Application/"]
COPY ["HotelServiceManagement.Domain/HotelServiceManagement.Domain.csproj", "HotelServiceManagement.Domain/"]
COPY ["HotelServiceManagement.Infrastructure/HotelServiceManagement.Infrastructure.csproj", "HotelServiceManagement.Infrastructure/"]
RUN dotnet restore "HotelServiceManagement.Api/HotelServiceManagement.Api.csproj"

# Copy toàn bộ code và publish
COPY . .
WORKDIR "/src/HotelServiceManagement.Api"
RUN dotnet publish "HotelServiceManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HotelServiceManagement.Api.dll"]
