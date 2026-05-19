#!/bin/bash

echo "Ejecutando test"

rm -rf tests/TestResults coveragereport

echo "Ejecutando tests de Auth"
dotnet test tests/PixPro.Auth.UnitTests
dotnet test tests/PixPro.Auth.UnitTests --collect:"XPlat Code Coverage" --results-directory tests/TestResults --verbosity quiet

echo "Ejecutando tests de Notifications"
dotnet test tests/PixPro.Notifications.UnitTests
dotnet test tests/PixPro.Notifications.UnitTests --collect:"XPlat Code Coverage" --results-directory tests/TestResults --verbosity quiet

echo "Ejecutando tests de Projects"
dotnet test tests/PixPro.Projects.UnitTests
dotnet test tests/PixPro.Projects.UnitTests --collect:"XPlat Code Coverage" --results-directory tests/TestResults --verbosity quiet

echo ""
echo "Generando reporte"

~/.dotnet/tools/reportgenerator -reports:"tests/TestResults/**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:"Html;HtmlSummary;Xml;SonarQube" -verbosity:Warning

echo ""
echo "Ubicación: coveragereport/index.html"

xdg-open coveragereport/index.html