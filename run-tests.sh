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

echo "Ejecutando tests de IA"

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

VENV_DIR="$PROJECT_ROOT/tests/ia_python_venv"

if [ ! -d "$VENV_DIR" ]; then
    python3 -m venv "$VENV_DIR" > /dev/null 2>&1
    "$VENV_DIR/bin/pip" install --upgrade pip -q
    "$VENV_DIR/bin/pip" install pika==1.3.2 pydantic==2.11.3 fastapi==0.115.12 uvicorn==0.34.3 python-dotenv==1.1.0 requests==2.32.3 sqlalchemy==2.0.40 psycopg2-binary==2.9.10 alembic==1.15.2 cloudinary==1.42.2 openai==1.78.0 pillow==11.2.1 aio-pika==9.5.5 -q
    "$VENV_DIR/bin/pip" install pytest pytest-cov pytest-mock -q
fi

cd tests/ia_python_tests
if [ ! -f "app/services/guardrails.py" ]; then
    cp -r ../../src/Services/IA/app . > /dev/null 2>&1
    touch app/__init__.py
    touch app/services/__init__.py
    touch app/models/__init__.py
    touch app/processors/__init__.py
    touch app/infrastructure/__init__.py
    
    echo "" > app/__init__.py
    echo "" > app/services/__init__.py
    echo "" > app/models/__init__.py
    echo "" > app/processors/__init__.py
    echo "" > app/infrastructure/__init__.py
fi

"$VENV_DIR/bin/python" -m pytest test_all.py -q --tb=line -W ignore \
  --cov=app \
  --cov-report=xml:coverage.xml \
  --cov-report=term

if [ -f "coverage.xml" ]; then
    cp coverage.xml ../TestResults/ia_coverage.xml
fi

rm -rf app

cd ../..

echo ""
echo "Generando reporte"

COVERAGE_REPORTS="tests/TestResults/**/coverage.cobertura.xml"
if [ -f "tests/TestResults/ia_coverage.xml" ]; then
    COVERAGE_REPORTS="$COVERAGE_REPORTS;tests/TestResults/ia_coverage.xml"
fi

~/.dotnet/tools/reportgenerator \
  -reports:"$COVERAGE_REPORTS" \
  -targetdir:"coveragereport" \
  -reporttypes:"Html;HtmlSummary;Xml;SonarQube" \
  -verbosity:Warning \
  -assemblyfilters:"-Infrastructure" \
  -classfilters:"-*.DependencyInjection;-*.DependencyInjection?;-*.*.DependencyInjection;-PixPro.Services.*.Application.DTOs.*;-PixPro.Services.*.Domain.Events.*;-PixPro.Services.*.Infrastructure.*;-PixPro.Services.Projects.Application.Services.ProjectService;-PixPro.Services.Projects.Domain.Entities.Project;-PixPro.Services.Projects.Domain.ValueObjects.*;-app.infrastructure.*;-app.main;-app.models.database;-app.services.database_service;-app.processors.openai_processor;-app.processors.pixazo;-app.processors.pollinations" \
  -minimumCoverage:1 > /dev/null 2>&1

echo ""
echo "Ubicación: coveragereport/index.html"

xdg-open coveragereport/index.html
