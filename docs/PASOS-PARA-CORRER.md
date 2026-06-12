# Guía Paso a Paso: Cómo Correr Todo el Sistema

## 📋 Requisitos Previos

1. .NET 9 SDK instalado
2. Docker y Docker Compose instalados
3. Archivo `.env` creado en la raíz del proyecto (ver ejemplo abajo)


---

## 🚀 OPCIÓN 1: Desarrollo Local (Sin Docker)

### Paso 1: Levantar solo las bases de datos en Docker

```bash
cd /home/target-folder
docker-compose up -d db projects-db notifications-db rabbitmq
```

Verifica que estén corriendo:
```bash
docker-compose ps
```

### Paso 2: Compilar todos los proyectos

```bash
cd /home/target-folder
dotnet build
```

### Paso 3: Correr cada microservicio en terminales separadas

**Terminal 1 - Auth Service:**
```bash
cd /home/target-folder
dotnet run
```
Debe decir: `Now listening on: http://localhost:5001`

**Terminal 2 - Projects Service:**
```bash
cd /home/target-folder
dotnet run
```
Debe decir: `Now listening on: http://localhost:5002`

**Terminal 3 - Notifications Service:**
```bash
cd /home/target-folder
dotnet run
```
Debe decir: `Now listening on: http://localhost:5003`

**Terminal 4 - Gateway:**
```bash
cd /home/target-folder
dotnet run
```

**Terminal 5 - IA**
```bash
cd /home/target-folder
dotnet run
```

Debe decir: `Now listening on: http://localhost:5000`

### Paso 4: Probar el sistema

Abre tu navegador o Postman:

**1. Health Check del Gateway:**
```
GET http://localhost:5000/health
```
Debe retornar: `Healthy`

**2. Registrar un usuario:**
```
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#",
  "firstName": "Test",
  "lastName": "User"
}
```

**3. Login:**
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#"
}
```
Copia el `token` de la respuesta.

**4. Probar endpoint protegido:**
```
GET http://localhost:5000/api/projects
Authorization: Bearer TU_TOKEN_AQUI
```

### Paso 5: Detener todo

Presiona `Ctrl+C` en cada terminal, luego:
```bash
docker compose down
```

---

## 🐳 OPCIÓN 2: Todo en Docker

### Paso 1: Construir las imágenes Docker

```bash
cd /home/target-folder

# Construir Gateway
docker compose build 
```

Esto puede tardar varios minutos la primera vez.

### Paso 2: Levantar todo el sistema

```bash
cd /home/jhon-rivera/University/sd5/service
docker compose up -d
```

### Paso 4: Ver los logs

```bash
# Ver todos los logs
docker-compose logs -f

# Ver solo Gateway
docker-compose logs -f gateway

# Ver solo Auth
docker-compose logs -f auth
```

### Paso 5: Verificar que todo esté corriendo

```bash
docker-compose ps
```

Todos los servicios deben mostrar estado `Up`.

### Paso 6: Probar el sistema

**IMPORTANTE**: En Docker, el Gateway corre en puerto **8080** (no 5000)

**1. Health Check:**
```
GET http://localhost:8080/health
```

**2. Registrar usuario:**
```
POST http://localhost:8080/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#",
  "firstName": "Test",
  "lastName": "User"
}
```

**3. Login y obtener token:**
```
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!@#"
}
```

**4. Probar endpoint protegido:**
```
GET http://localhost:8080/api/projects
Authorization: Bearer TU_TOKEN_AQUI
```

### Paso 7: Ver logs de errores (si algo falla)

```bash
# Ver últimas 50 líneas de logs del Gateway
docker-compose logs --tail=50 gateway

# Ver logs de Auth
docker-compose logs --tail=50 auth

# Ver logs de Projects
docker-compose logs --tail=50 projects
```

### Paso 8: Detener y limpiar

```bash
# Detener todos los contenedores
docker-compose down

# Detener y eliminar volúmenes (borra las bases de datos)
docker-compose down -v
```
---

## ☁️ OPCIÓN 3: Despliegue en Servidor (Coolify + Hetzner)

### Requisitos

1. Servidor VPS (Hetzner CX23 recomendado: 2 vCPU, 4GB RAM, ~$6.5/mes)
2. Dominio propio (ej: `pixpro.lat`)
3. Cuenta de GitHub con el repositorio del proyecto

### Paso 1: Configurar el servidor Hetzner

1. Crear un servidor en [Hetzner Cloud](https://console.hetzner.cloud/)
   - Imagen: **Ubuntu 22.04+**
   - Tipo: **CX23** (2 vCPU, 4GB RAM)
   - Ubicación: la más cercana a tus usuarios
2. Acceder por SSH:
```bash
ssh root@<IP_DEL_SERVIDOR>
```
3. Configurar firewall:
```bash
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP
ufw allow 443/tcp   # HTTPS
ufw enable
```

### Paso 2: Instalar Coolify

```bash
curl -fsSL https://cdn.coollabs.io/coolify/install.sh | bash
```

Espera a que termine la instalación. Luego accede al dashboard:
```
http://<IP_DEL_SERVIDOR>:18473
```

Crea tu cuenta de administrador en el primer acceso.

### Paso 3: Conectar repositorio de GitHub

1. En Coolify → **Sources** → **Add GitHub App**
2. Sigue las instrucciones para crear una GitHub App y conectar tu repositorio privado
3. Da permisos al repositorio `capstone-software-development-5-service`

### Paso 4: Crear el recurso en Coolify

1. **Projects** → Crear proyecto → Crear entorno (ej: `production`)
2. **Add Resource** → **Docker Compose**
3. Selecciona el repositorio y branch `develop`
4. En **Docker Compose Location**: `docker-compose.coolify.yml`
5. Clic en **Save**

### Paso 5: Configurar variables de entorno

En Coolify → tu recurso → **Environment Variables**, agrega todas las variables del archivo `.env`:

```
JWT_SECRET=<tu_secret_de_32+_caracteres>
JWT_ISSUER=PixProAuth
JWT_AUDIENCE=PixProAPI
AUTH0_DOMAIN=<tu_dominio_auth0>
AUTH0_AUDIENCE=<tu_audience_auth0>
POSTGRES_PASSWORD=<password>
MONGO_INITDB_ROOT_USERNAME=<usuario>
MONGO_INITDB_ROOT_PASSWORD=<password>
RABBITMQ_DEFAULT_USER=<usuario>
RABBITMQ_DEFAULT_PASS=<password>
OPENAI_API_KEY=<tu_api_key>
```

### Paso 6: Configurar dominio y SSL

1. En tu proveedor de dominios (ej: Namecheap), crear un **A Record**:
   - Host: `api`
   - Value: `<IP_DEL_SERVIDOR>`
   - TTL: Automatic
2. En Coolify → tu recurso → **Configuration** → **Domains for gateway**:
   ```
   https://api.tu-dominio.com
   ```
3. Clic en **Save**

Coolify emitirá automáticamente un certificado SSL de Let's Encrypt.

### Paso 7: Configurar red de Coolify

En Coolify → tu recurso → **Configuration** → **Advanced**:
- Activar **"Connect To Predefined Network"** ✅

Esto permite que Traefik (el proxy de Coolify) alcance los contenedores de tu aplicación.

### Paso 8: Desplegar

1. Clic en **Redeploy** en Coolify
2. Espera a que todos los servicios se construyan y arranquen (~5-10 min la primera vez)
3. Verifica el estado en la pestaña **Deployments** o **Logs**

### Paso 9: Verificar el despliegue

```bash
# Health check
curl https://api.tu-dominio.com/health
# Debe retornar: Healthy

# Verificar certificado SSL
curl -vI https://api.tu-dominio.com/health 2>&1 | grep issuer
# Debe decir: issuer: ... Let's Encrypt

# WebSocket
# Conectar a: wss://api.tu-dominio.com/api/websocket/connect
```

### Gestión del servidor

**Ver estado de los contenedores:**
```bash
ssh root@<IP_DEL_SERVIDOR>
docker ps
```

**Ver consumo de recursos:**
```bash
docker stats --no-stream
free -h
```

**Apagar el servidor (desde SSH):**
```bash
# IMPORTANTE: NO usar docker stop, solo poweroff directamente
# Si usas docker stop, los contenedores no arrancan automáticamente al reiniciar
sudo poweroff
```

**Encender el servidor:**
1. Hetzner Cloud → tu servidor → **Actions** → **Power on**
2. Los contenedores se reinician automáticamente (~1-2 min)

**Si Coolify no arranca tras reiniciar:**
```bash
ssh root@<IP_DEL_SERVIDOR>
bash /data/coolify/source/upgrade.sh
```

**Reiniciar solo la aplicación (sin rebuild):**
- Coolify UI → tu recurso → **Restart**

**Redesplegar con cambios nuevos:**
- Push a `develop` → Coolify UI → **Redeploy**

### Puertos en Coolify

- Gateway: `https://api.tu-dominio.com` (Traefik maneja SSL y routing)
- Auth, Projects, Notifications, IA: **solo accesibles internamente** entre contenedores
- Coolify Dashboard: `http://<IP_DEL_SERVIDOR>:18473`

### Notas importantes

- Hetzner **cobra igual** con el servidor apagado. Solo se deja de cobrar al **eliminar** el servidor
- El archivo de despliegue es `docker-compose.coolify.yml` (sin puertos de bases de datos expuestos)
- Las migraciones de base de datos se aplican automáticamente al iniciar los servicios
- Los certificados SSL se renuevan automáticamente vía Let's Encrypt

---

## 🔍 Troubleshooting

### Error: "Port already in use"
```bash
# Ver qué está usando el puerto
sudo lsof -i :5000
sudo lsof -i :8080

# Matar el proceso
kill -9 PID
```

### Error: "Cannot connect to database"
```bash
# Verificar que las bases de datos estén corriendo
docker-compose ps db projects-db notifications-db

# Ver logs de la base de datos
docker-compose logs db
```

### Error: "JWT validation failed"
- Verifica que `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` sean exactamente iguales en `.env`
- El secret debe tener mínimo 32 caracteres

### Los microservicios no se pueden comunicar en Docker
- Verifica que todos estén en la red `pixpro_net`
- Usa nombres de servicio, no localhost: `http://auth:8081` (no `http://localhost:8081`)

---

## 📝 Resumen de Puertos

### Desarrollo Local (sin Docker):
- Gateway: `http://localhost:5000`
- Auth: `http://localhost:5001` (solo para debugging)
- Projects: `http://localhost:5002` (solo para debugging)
- Notifications: `http://localhost:5003` (solo para debugging)

### Docker:
- Gateway: `http://localhost:8080` ✅ ÚNICO PUNTO DE ENTRADA
- Auth, Projects, Notifications: NO accesibles desde fuera

### Bases de Datos (Local y Docker):
- PostgreSQL (Auth): `localhost:5432`
- PostgreSQL (Projects): `localhost:5433`
- MongoDB (Notifications): `localhost:27017`
- RabbitMQ Management: `http://localhost:15672`

### Coolify (Servidor):
- Gateway: `https://api.tu-dominio.com` (único punto de entrada público)
- Auth, Projects, Notifications, IA: **solo accesibles internamente**
- Bases de datos: **sin puertos expuestos** (solo accesibles entre contenedores)
- Coolify Dashboard: `http://<IP_DEL_SERVIDOR>:18473`

---

## ✅ Checklist de Validación

**Desarrollo Local:**
- [ ] Bases de datos corriendo en Docker
- [ ] Auth Service corriendo (puerto 5001)
- [ ] Projects Service corriendo (puerto 5002)
- [ ] Notifications Service corriendo (puerto 5003)
- [ ] Gateway corriendo (puerto 5000)
- [ ] Health check funciona: `GET http://localhost:5000/health`
- [ ] Puedo registrar usuario
- [ ] Puedo hacer login y obtener token
- [ ] Puedo acceder a endpoints protegidos con token

**Docker:**
- [ ] Todas las imágenes construidas
- [ ] Archivo `.env` creado con todas las variables
- [ ] `docker-compose up -d` ejecutado sin errores
- [ ] Todos los servicios muestran estado `Up`
- [ ] Health check funciona: `GET http://localhost:8080/health`
- [ ] Puedo registrar usuario
- [ ] Puedo hacer login y obtener token
- [ ] Puedo acceder a endpoints protegidos con token

**Coolify (Servidor):**
- [ ] Servidor Hetzner creado y accesible por SSH
- [ ] Coolify instalado y dashboard accesible
- [ ] Repositorio de GitHub conectado
- [ ] Variables de entorno configuradas en Coolify
- [ ] Dominio configurado con A Record apuntando al servidor
- [ ] "Connect To Predefined Network" activado
- [ ] Despliegue exitoso (todos los servicios `Running`)
- [ ] Health check funciona: `GET https://api.tu-dominio.com/health`
- [ ] Certificado SSL válido (Let's Encrypt)
- [ ] WebSocket funciona: `wss://api.tu-dominio.com/api/websocket/connect`
