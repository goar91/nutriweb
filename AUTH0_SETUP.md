# Guía de Configuración de Auth0 para NutriWeb

## 📝 Instrucciones Paso a Paso

### Paso 1: Crear Cuenta en Auth0

1. Ve a [https://auth0.com/signup](https://auth0.com/signup)
2. Regístrate con tu email o cuenta de Google/GitHub
3. Verifica tu email
4. Crea un **Tenant** (por ejemplo: `nutriweb-dev` o `tu-nombre-nutriweb`)

### Paso 2: Configurar Single Page Application

1. En el Dashboard de Auth0, ve al menú lateral izquierdo
2. Click en **Applications** > **Applications**
3. Click en **Create Application**
4. Configura:
   - **Name**: `NutriWeb Frontend`
   - **Application Type**: Selecciona **Single Page Web Applications**
   - Click **Create**

5. En la pestaña **Settings**, configura lo siguiente:

   **Application URIs:**
   ```
   Allowed Callback URLs:
   http://localhost:4200, http://localhost:54107

   Allowed Logout URLs:
   http://localhost:4200, http://localhost:54107

   Allowed Web Origins:
   http://localhost:4200, http://localhost:54107

   Allowed Origins (CORS):
   http://localhost:4200, http://localhost:54107
   ```

6. Scroll hasta abajo y click **Save Changes**

7. **IMPORTANTE: Guarda estos valores** (los vas a necesitar):
   - **Domain** (ejemplo: `nutriweb-dev.us.auth0.com`)
   - **Client ID** (ejemplo: `abc123def456ghi789jkl`)

### Paso 3: Configurar la API

1. En el menú lateral, ve a **Applications** > **APIs**
2. Click en **Create API**
3. Configura:
   - **Name**: `NutriWeb API`
   - **Identifier**: `https://nutriweb.api` (este es tu audience)
   - **Signing Algorithm**: `RS256`
4. Click **Create**

5. **IMPORTANTE: Guarda este valor**:
   - **Identifier**: `https://nutriweb.api` (este será tu audience)

### Paso 4: Configurar el Frontend

Edita el archivo `frontend/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  auth0: {
    domain: 'TU_DOMINIO.auth0.com',  // ← Reemplaza aquí
    clientId: 'TU_CLIENT_ID',         // ← Reemplaza aquí
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://nutriweb.api'  // ← Tu API identifier
    }
  },
  apiUrl: 'http://localhost:5000/api'
};
```

**Ejemplo con valores reales:**
```typescript
export const environment = {
  production: false,
  auth0: {
    domain: 'nutriweb-dev.us.auth0.com',
    clientId: 'Xy9Z8a7B6c5D4e3F2g1H0i',
    authorizationParams: {
      redirect_uri: window.location.origin,
      audience: 'https://nutriweb.api'
    }
  },
  apiUrl: 'http://localhost:5000/api'
};
```

### Paso 5: Configurar el Backend

Edita el archivo `backend/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "NutritionDb": "Host=localhost;Port=5432;Database=nutriciondb;Username=postgres;Password=postgres;Pooling=true;Trust Server Certificate=true"
  },
  "Auth0": {
    "Domain": "TU_DOMINIO.auth0.com",    // ← Reemplaza aquí
    "Audience": "https://nutriweb.api"   // ← Tu API identifier
  }
}
```

**Ejemplo con valores reales:**
```json
{
  "Auth0": {
    "Domain": "nutriweb-dev.us.auth0.com",
    "Audience": "https://nutriweb.api"
  }
}
```

### Paso 6: Crear un Usuario de Prueba (Opcional)

Para probar sin crear una cuenta nueva cada vez:

1. En Auth0 Dashboard, ve a **User Management** > **Users**
2. Click **Create User**
3. Configura:
   - **Email**: `test@nutriweb.com` (o el que prefieras)
   - **Password**: Crea una contraseña segura
   - **Connection**: `Username-Password-Authentication`
4. Click **Create**

### Paso 7: Verificar la Configuración

1. **Inicia el Backend**:
   ```bash
   cd backend
   dotnet run
   ```
   Debería ver: `Servidor en http://localhost:5000/`

2. **Inicia el Frontend**:
   ```bash
   cd frontend
   npm start
   ```
   Debería ver: `Local: http://localhost:4200/`

3. **Prueba el Login**:
   - Abre `http://localhost:4200`
   - Deberías ver la pantalla de login
   - Click en "Iniciar sesión con Auth0"
   - Se abrirá la página de Auth0
   - Inicia sesión con tu usuario de prueba o crea uno nuevo
   - Deberías ser redirigido al Dashboard

## ⚠️ Problemas Comunes

### Error: "Invalid Callback URL"

**Solución**: Verifica que en Auth0 Dashboard, en la configuración de tu aplicación, hayas agregado correctamente las URLs de callback.

### Error: "Audience is invalid"

**Solución**: Asegúrate de que el `audience` en `environment.ts` coincida exactamente con el `Identifier` de tu API en Auth0.

### Error: "Domain is invalid"

**Solución**: El dominio debe incluir `.auth0.com` al final. Ejemplo: `nutriweb-dev.us.auth0.com`

### El login funciona pero falla al llamar a la API

**Solución**: 
1. Verifica que el backend esté corriendo
2. Revisa la configuración de CORS en `Program.cs`
3. Asegúrate de que las URLs permitidas incluyan tu frontend

## 🔒 Seguridad en Producción

Cuando subas a producción:

1. **Actualiza las URLs en Auth0**:
   - Agrega tu dominio de producción (ejemplo: `https://nutriweb.com`)
   - Mantén las de desarrollo separadas si es necesario

2. **Actualiza `environment.prod.ts`**:
   ```typescript
   export const environment = {
     production: true,
     auth0: {
       domain: 'TU_DOMINIO.auth0.com',
       clientId: 'TU_CLIENT_ID_PRODUCCION',
       authorizationParams: {
         redirect_uri: 'https://nutriweb.com',
         audience: 'https://nutriweb.api'
       }
     },
     apiUrl: 'https://api.nutriweb.com/api'
   };
   ```

3. **Variables de entorno en el backend**:
   - No guardes secretos en `appsettings.json`
   - Usa variables de entorno o Azure Key Vault

## 📚 Recursos Adicionales

- [Documentación oficial de Auth0](https://auth0.com/docs)
- [Auth0 Angular SDK](https://github.com/auth0/auth0-angular)
- [Auth0 .NET SDK](https://auth0.com/docs/quickstart/backend/aspnet-core-webapi)

## 💡 Tips

1. **Desarrollo Local**: Usa `http://localhost` en las URLs
2. **Testing**: Crea múltiples usuarios de prueba en Auth0
3. **Logs**: Revisa los logs en Auth0 Dashboard > Monitoring > Logs
4. **Tokens**: Los tokens JWT tienen una expiración de 24 horas por defecto

## ✅ Checklist de Configuración

- [ ] Cuenta de Auth0 creada
- [ ] Tenant configurado
- [ ] SPA Application creada en Auth0
- [ ] API creada en Auth0
- [ ] Callback URLs configuradas
- [ ] `environment.ts` actualizado con Domain y Client ID
- [ ] `appsettings.json` actualizado con Domain y Audience
- [ ] Backend corriendo sin errores
- [ ] Frontend corriendo sin errores
- [ ] Login funcionando correctamente
- [ ] Usuario puede acceder al Dashboard
- [ ] Formulario de historias clínicas funciona

---

¿Tienes problemas? Revisa los logs en la consola del navegador (F12) y los logs del backend.
