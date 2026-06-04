import sys
from pathlib import Path

# Add current directory to Python path for local imports
current_dir = Path(__file__).parent
sys.path.insert(0, str(current_dir))

# Add the Services/IA source directory to Python path
source_path = Path("/home/acer/Documentos/workspace/DS5/capstone/service/src/Services/IA")
sys.path.insert(0, str(source_path))
