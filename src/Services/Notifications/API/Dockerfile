FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8083
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Services/Notifications/API/API.csproj", "src/Services/Notifications/API/"]
COPY ["src/Services/Notifications/Application/Application.csproj", "src/Services/Notifications/Application/"]
COPY ["src/Services/Notifications/Domain/Domain.csproj", "src/Services/Notifications/Domain/"]
COPY ["src/Services/Notifications/Infrastructure/Infrastructure.csproj", "src/Services/Notifications/Infrastructure/"]

RUN dotnet restore "src/Services/Notifications/API/API.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/Services/Notifications/API"
RUN dotnet build "API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8083

ENTRYPOINT ["dotnet", "API.dll"]
