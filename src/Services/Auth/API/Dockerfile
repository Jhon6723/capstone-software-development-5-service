FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared/PixPro.Shared/PixPro.Shared.csproj", "Shared/PixPro.Shared/"]
COPY ["src/Services/Auth/API/API.csproj", "Auth/API/"]
RUN dotnet restore "Auth/API/API.csproj"
COPY src/ .
WORKDIR /src/Services/Auth/API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENTRYPOINT ["dotnet", "API.dll"]