#!/bin/bash
echo "Ejecutando tests..."
dotnet test
##dotnet test tests/PixPro.UnitTests/PixPro.UnitTests.csproj --collect:"XPlat Code Coverage" --verbosity quiet

echo "Generando reporte..."
reportgenerator -reports:"tests/PixPro.UnitTests/TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html -verbosity:Warning

echo "Abriendo reporte..."
xdg-open coveragereport/index.html
