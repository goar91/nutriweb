# NutriWeb - Plataforma de Gestión Nutricional

## 🚀 Características

- ✅ **Sistema de autenticación** con Auth0
- ✅ **Dashboard interactivo** para gestión de pacientes
- ✅ **Formulario completo** de historia clínica nutricional
- ✅ **Diseño responsive** y moderno
- ✅ **Backend optimizado** en .NET 10
- ✅ **Frontend en Angular 21** con standalone components

## 📋 Requisitos Previos

- Node.js 18+ y npm
- .NET 10 SDK
- PostgreSQL 14+
- Cuenta en Auth0 (gratuita)

## 🔧 Configuración de Auth0

### 1. Crear cuenta en Auth0

1. Ve a [auth0.com](https://auth0.com) y crea una cuenta gratuita
2. Crea un nuevo **Tenant** (por ejemplo: `nutriweb-dev`)

### 2. Configurar la Aplicación (SPA)

1. En el dashboard de Auth0, ve a **Applications** > **Create Application**
2. Nombre: `NutriWeb Frontend`
3. Tipo: **Single Page Web Applications**
4. Click en **Create**
5. En la pestaña **Settings**, configura:
   - **Allowed Callback URLs**: `http://localhost:4200, http://localhost:54107`
   - **Allowed Logout URLs**: `http://localhost:4200, http://localhost:54107`
   - **Allowed Web Origins**: `http://localhost:4200, http://localhost:54107`
6. Guarda los cambios
7. **Copia** el `Domain` y `Client ID` para usarlos después

### 3. Configurar la API

1. En el dashboard de Auth0, ve a **Applications** > **APIs** > **Create API**
2. Nombre: `NutriWeb API`
3. Identifier: `https://nutriweb.api` (este será tu audience)
4. Signing Algorithm: **RS256**
5. Click en **Create**
6. **Copia** el `Identifier` para usarlo después

## ⚙️ Instalación

### Backend (.NET)

```bash
cd backend
dotnet restore
```

Edita `appsettings.json` y reemplaza los valores de Auth0:

```json
{
  "Auth0": {
    "Domain": "TU_DOMINIO.auth0.com",
    "Audience": "https://nutriweb.api"
  }
}
```

Ejecuta el backend:

```bash
dotnet run
```

El backend estará disponible en `http://localhost:5000`

### Frontend (Angular)

```bash
cd frontend
npm install
```

Edita `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  auth0: {
    domain: 'TU_DOMINIO.auth0.com',
    clientId: 'TU_CLIENT_ID',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://nutriweb.api'
    }
  },
  apiUrl: 'http://localhost:5000/api'
};
```

Ejecuta el frontend:

```bash
npm start
```

El frontend estará disponible en `http://localhost:4200` o `http://localhost:54107`

## 🗄️ Base de Datos

### PostgreSQL

La aplicación creará automáticamente la tabla necesaria al iniciar. Solo asegúrate de tener PostgreSQL corriendo:

```sql
-- Crear base de datos
CREATE DATABASE nutriciondb;

-- La tabla se creará automáticamente al ejecutar la aplicación
```

## 🎨 Estructura del Proyecto

```
nutriweb/
├── backend/              # API en .NET 10
│   ├── Program.cs       # Configuración y endpoints
│   ├── backend.csproj   # Dependencias
│   └── appsettings.json # Configuración
│
└── frontend/            # App Angular 21
    ├── src/
    │   ├── app/
    │   │   ├── components/
    │   │   │   ├── login/        # Componente de login
    │   │   │   ├── dashboard/    # Dashboard principal
    │   │   │   └── navbar/       # Barra de navegación
    │   │   ├── app.ts           # Formulario de historias
    │   │   ├── app.config.ts    # Configuración de Auth0
    │   │   └── nutrition.service.ts
    │   └── environments/        # Variables de entorno
    └── package.json
```

## 🚀 Uso

1. **Inicia sesión**: Accede a `http://localhost:4200` y haz clic en "Iniciar sesión con Auth0"
2. **Dashboard**: Después de autenticarte, verás el dashboard con estadísticas
3. **Nueva Historia**: Click en "Nueva Historia Clínica" para registrar pacientes
4. **Formulario**: Completa todos los campos necesarios y guarda

## 🔒 Seguridad

- Autenticación JWT con Auth0
- CORS configurado para desarrollo
- Validación de datos en backend
- Tokens seguros en frontend

## 📱 Responsive Design

La aplicación está completamente optimizada para:
- 📱 Móviles (320px+)
- 📱 Tablets (768px+)
- 💻 Desktop (1024px+)
- 🖥️ Pantallas grandes (1400px+)

## 🎯 Próximas Características

- [ ] Listado de pacientes
- [ ] Búsqueda y filtros
- [ ] Reportes en PDF
- [ ] Gráficas estadísticas
- [ ] Notificaciones
- [ ] Modo oscuro completo

## 🐛 Troubleshooting

### Error: "No se puede conectar con el backend"

Asegúrate de que:
1. El backend esté corriendo en `http://localhost:5000`
2. PostgreSQL esté activo
3. Las configuraciones de CORS sean correctas

### Error: "Auth0 configuration is missing"

Verifica que:
1. Hayas configurado correctamente `environment.ts`
2. Los valores de Domain y Client ID sean correctos
3. Las URLs de callback estén configuradas en Auth0

### El frontend no carga

1. Ejecuta `npm install` nuevamente
2. Verifica que no haya errores de compilación
3. Limpia la caché: `npm cache clean --force`

## 📄 Licencia

Este proyecto es privado y de uso educativo.

## 👨‍💻 Desarrollado con

- Angular 21
- .NET 10
- Auth0
- PostgreSQL
- TypeScript
- SCSS
