# Cloudflare Tunnel - Guía Rápida

Exponer tu backend local con URL pública gratuita usando Cloudflare Tunnel.

## Requisitos

- `cloudflared` instalado
- Backend corriendo en `localhost:8080`

## Comandos Screen

### Iniciar túnel en segundo plano
```bash
screen -S tunnel -dm cloudflared tunnel --url http://localhost:8080
```

### Ver logs y obtener URL
```bash
screen -r tunnel
```
Busca la línea que contiene `https://xxxx.trycloudflare.com`

### Salir de la sesión (sin matar el túnel)
Presiona: `Ctrl+A` luego `D`

### Ver sesiones activas
```bash
screen -ls
```

### Matar el túnel
```bash
screen -X -S tunnel quit
```

## Flujo completo

```bash
# 1. Iniciar túnel
screen -S tunnel -dm cloudflared tunnel --url http://localhost:8080

# 2. Esperar 3 segundos y ver la URL
sleep 3 && screen -r tunnel

# 3. Presiona Ctrl+A, D para salir
# El túnel sigue corriendo

# 4. Más tarde, reconectar para ver logs
screen -r tunnel
```

## Notas

- La URL cambia cada vez que reinicias el túnel
- Formato típico: `https://xxxx.trycloudflare.com`
- Swagger disponible en: `https://xxxx.trycloudflare.com/swagger/index.html`

## Troubleshooting

| Problema | Solución |
|----------|----------|
| No veo la URL | `screen -r tunnel` y usa `Shift+PageUp` para scroll |
| Túnel no inicia | Verifica que backend esté en `localhost:8080` |
| Sesión colgada | `screen -X -S tunnel quit` y reinicia |

## Alternativa con log en archivo

```bash
# Mata sesión actual si existe
screen -X -S tunnel quit 2>/dev/null

# Inicia con log a archivo
screen -S tunnel -dm bash -c 'cloudflared tunnel --url http://localhost:8080 2>&1 | tee ~/tunnel.log'

# Ver URL directamente
grep -i "https://" ~/tunnel.log | head -3
```
