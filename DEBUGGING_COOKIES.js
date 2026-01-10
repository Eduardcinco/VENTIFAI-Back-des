/**
 * ============================================================================
 * DEBUGGING COOKIES Y CREDENTIALS - FLUJO COMPLETO
 * ============================================================================
 * 
 * Problema: /api/auth/refresh devuelve 400 Bad Request
 * Causa probable: El navegador NO está enviando la cookie refresh_token
 * 
 * ============================================================================
 * PASO 1: VERIFICAR EN BROWSER DEVTOOLS
 * ============================================================================
 * 
 * 1. Abrir DevTools → Aplication → Cookies → seleccionar tu dominio frontend
 * 2. ¿Existen las cookies "access_token" y "refresh_token"?
 *    Si NO → El backend no las está creando correctamente
 *    Si SÍ → Proceder al paso 2
 * 
 * 3. Click en la cookie "refresh_token" y verificar:
 *    - Domain: debe contener tu dominio (ej: phenomenal-strudel-befb4f.netlify.app)
 *    - Path: /
 *    - Secure: ✓ (porque es HTTPS)
 *    - SameSite: None
 *    - HttpOnly: ✓ (solo lectura por HTTP, no por JS)
 * 
 * ============================================================================
 * PASO 2: VERIFICAR EN NETWORK TAB (Request actual)
 * ============================================================================
 * 
 * 1. Abrir DevTools → Network
 * 2. Hacer click a POST /api/auth/refresh
 * 3. Verificar los HEADERS:
 * 
 *    REQUEST HEADERS:
 *    - Cookie: access_token=...; refresh_token=... ← DEBE ESTAR AQUÍ
 *    - Origin: https://phenomenal-strudel-befb4f.netlify.app
 * 
 *    RESPONSE HEADERS:
 *    - Set-Cookie: access_token=...; Path=/; Secure; SameSite=None; HttpOnly
 *    - Set-Cookie: refresh_token=...; Path=/; Secure; SameSite=None; HttpOnly
 *    - Access-Control-Allow-Credentials: true ← CRÍTICO
 *    - Access-Control-Allow-Origin: https://phenomenal-strudel-befb4f.netlify.app
 * 
 * ============================================================================
 * PASO 3: CÓDIGO ANGULAR - VERIFICAR withCredentials
 * ============================================================================
 * 
 * El frontend DEBE usar withCredentials: true en TODAS las peticiones:
 * 
 * ❌ INCORRECTO:
 * this.http.post('https://tu-backend.com/api/auth/refresh', {}).subscribe(...)
 * 
 * ✅ CORRECTO:
 * this.http.post('https://tu-backend.com/api/auth/refresh', {}, {
 *   withCredentials: true  // ← OBLIGATORIO para cookies cross-origin
 * }).subscribe(...)
 * 
 * O configurable globalmente con un HttpInterceptor:
 * 
 * @Injectable()
 * export class CredentialsInterceptor implements HttpInterceptor {
 *   intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
 *     // Agregar withCredentials a TODAS las peticiones
 *     const credentialsReq = req.clone({
 *       withCredentials: true
 *     });
 *     return next.handle(credentialsReq);
 *   }
 * }
 * 
 * Luego registrarlo en app.module.ts:
 * providers: [
 *   { provide: HTTP_INTERCEPTORS, useClass: CredentialsInterceptor, multi: true }
 * ]
 * 
 * ============================================================================
 * PASO 4: VERIFICAR BACKEND (YA COMPLETADO)
 * ============================================================================
 * 
 * ✅ CORS configurado correctamente:
 *    - AllowCredentials() → emite Access-Control-Allow-Credentials: true
 *    - WithOrigins(...) → permite origen específico
 *    - AllowAnyMethod() → permite POST, OPTIONS, etc.
 * 
 * ✅ Cookie Policy configurado:
 *    - SameSite = None → permite cross-origin
 *    - Secure = Always → solo HTTPS
 *    - HttpOnly = true → no accesible desde JavaScript
 * 
 * ✅ Endpoint /refresh ahora tiene LOGGING DETALLADO:
 *    - Loguea todas las cookies recibidas
 *    - Loguea si encontró refresh_token en body o cookies
 *    - Loguea validación de token
 *    - Devuelve debug info en respuesta de error 400
 * 
 * ============================================================================
 * PASO 5: FLUJO ESPERADO DESPUÉS DE FIX
 * ============================================================================
 * 
 * 1. Usuario hace LOGIN:
 *    POST /api/auth/login con {correo, password}
 *    ↓
 *    Backend responde con:
 *    - Status: 200
 *    - Set-Cookie: access_token=...; SameSite=None; Secure; HttpOnly
 *    - Set-Cookie: refresh_token=...; SameSite=None; Secure; HttpOnly
 *    - Body: {accessToken, refreshToken, usuario}
 * 
 * 2. Navegador recibe Set-Cookie:
 *    ↓
 *    Almacena cookies (visibles en DevTools → Application → Cookies)
 * 
 * 3. Frontend llama a cualquier endpoint /api/...:
 *    POST /api/productos con {name: "test"}
 *    Headers incluyen: Cookie: access_token=...; refresh_token=...
 *    ↓
 *    Backend valida JWT desde access_token en cookie (middleware JWT)
 * 
 * 4. Cuando access_token expira (5-15 min), frontend llama:
 *    POST /api/auth/refresh (sin body)
 *    Headers incluyen: Cookie: access_token=...; refresh_token=...
 *    ↓
 *    Backend lee refresh_token de cookies, valida, genera nuevos tokens
 *    Responde con nuevas cookies Set-Cookie
 * 
 * ============================================================================
 * CHECKLIST DE DEBUGGING
 * ============================================================================
 * 
 * [ ] Cookies se crean después de /login (verificar en DevTools)
 * [ ] Cookies tienen SameSite=None
 * [ ] Cookies tienen Secure=true
 * [ ] Request a /refresh incluye Cookie header
 * [ ] Response de /refresh incluye Set-Cookie headers
 * [ ] Response de /refresh incluye Access-Control-Allow-Credentials: true
 * [ ] Frontend usa withCredentials: true
 * [ ] No hay errores en Console
 * [ ] Logs del backend muestran que recibe las cookies (en Development)
 * 
 * ============================================================================
 * LOGS DEL BACKEND ESPERADOS (Development)
 * ============================================================================
 * 
 * POST /api/auth/login:
 * 🔍 INCOMING REQUEST: POST /api/auth/login | Cookies: 0
 * 🍪 OUTGOING SET-COOKIE: 2 cookies
 *   📤 access_token=...
 *   📤 refresh_token=...
 * ✅ Access-Control-Allow-Credentials: true
 * 
 * POST /api/auth/refresh:
 * 🔍 INCOMING REQUEST: POST /api/auth/refresh | Cookies: 2
 *   📥 Cookie: access_token = eyJhbGc...
 *   📥 Cookie: refresh_token = eyJhbGc...
 * 🌐 Origin: https://phenomenal-strudel-befb4f.netlify.app
 * 🔄 REFRESH ENDPOINT CALLED
 *    Total cookies received: 2
 *    📥 Cookie: access_token = eyJhbGc...
 *    📥 Cookie: refresh_token = eyJhbGc...
 *    RefreshToken en body: False
 *    Intentando leer refresh_token de cookies...
 *    RefreshToken de cookie: True
 *    ✅ RefreshToken encontrado, buscando en DB...
 *    ✅ Validación exitosa. Generando nuevos tokens para usuarioId: 1
 *    ✅ Nuevos tokens guardados. Actualizando cookies...
 * 
 * ============================================================================
 * SI AÚN NO FUNCIONA
 * ============================================================================
 * 
 * 1. Compartir logs del backend (coloca en console de Railway)
 * 2. Captura de DevTools Network tab (request y response headers)
 * 3. Código del interceptor HTTP del frontend
 * 4. Verificar que el frontend está en el dominio correcto
 *    (debe ser: https://phenomenal-strudel-befb4f.netlify.app)
 */
