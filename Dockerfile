FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["TeamTaskManager.sln", "./"]
COPY ["src/backend/TeamTaskManager.Domain/TeamTaskManager.Domain.csproj", "src/backend/TeamTaskManager.Domain/"]
COPY ["src/backend/TeamTaskManager.Application/TeamTaskManager.Application.csproj", "src/backend/TeamTaskManager.Application/"]
COPY ["src/backend/TeamTaskManager.Infrastructure/TeamTaskManager.Infrastructure.csproj", "src/backend/TeamTaskManager.Infrastructure/"]
COPY ["src/backend/TeamTaskManager.Api/TeamTaskManager.Api.csproj", "src/backend/TeamTaskManager.Api/"]
RUN dotnet restore "src/backend/TeamTaskManager.Api/TeamTaskManager.Api.csproj"

COPY . .
RUN dotnet publish "src/backend/TeamTaskManager.Api/TeamTaskManager.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TeamTaskManager.Api.dll"]
