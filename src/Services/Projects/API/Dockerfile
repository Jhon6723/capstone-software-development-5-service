FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Shared/PixPro.Shared/PixPro.Shared.csproj", "Shared/PixPro.Shared/"]
COPY ["src/Services/Projects/API/API.csproj", "Projects/API/"]
RUN dotnet restore "Projects/API/API.csproj"
COPY src/ .
WORKDIR /src/Services/Projects/API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8082
ENTRYPOINT ["dotnet", "API.dll"]
