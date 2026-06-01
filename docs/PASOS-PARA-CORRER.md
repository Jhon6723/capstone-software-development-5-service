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

### Bases de Datos (ambos casos):
- PostgreSQL (Auth): `localhost:5432`
- PostgreSQL (Projects): `localhost:5433`
- MongoDB (Notifications): `localhost:27017`
- RabbitMQ Management: `http://localhost:15672`

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
