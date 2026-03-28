## ADDED Requirements

### Requirement: Login con email, password y tenantSlug
El sistema SHALL autenticar usuarios mediante email, password y tenantSlug. Si las credenciales son válidas y el tenant existe y está activo, SHALL emitir un access token JWT de corta duración y un refresh token opaco almacenado en base de datos.

#### Scenario: Login exitoso
- **WHEN** el usuario envía email válido, password correcta y tenantSlug existente
- **THEN** el sistema retorna `200 OK` con `accessToken`, `refreshToken`, `expiresIn`, `tokenType: "Bearer"` y el objeto `me` con datos del usuario

#### Scenario: Credenciales incorrectas
- **WHEN** el usuario envía password incorrecta para un email existente
- **THEN** el sistema retorna `401 Unauthorized` con código `INVALID_CREDENTIALS`

#### Scenario: Tenant no encontrado
- **WHEN** el usuario envía un tenantSlug que no corresponde a ningún tenant activo
- **THEN** el sistema retorna `404 Not Found` con código `TENANT_NOT_FOUND`

#### Scenario: Tenant suspendido
- **WHEN** el tenant existe pero tiene status `suspended` o `cancelled`
- **THEN** el sistema retorna `403 Forbidden` con código `TENANT_INACTIVE`

#### Scenario: Cuenta de usuario inactiva
- **WHEN** el usuario existe pero `is_active = false`
- **THEN** el sistema retorna `403 Forbidden` con código `ACCOUNT_SUSPENDED`

#### Scenario: Usuario sin rol en el tenant solicitado
- **WHEN** el usuario existe pero no tiene ningún rol en el tenant del tenantSlug
- **THEN** el sistema retorna `403 Forbidden` con código `INVALID_CREDENTIALS`

---

### Requirement: Renovación de access token via refresh token
El sistema SHALL permitir renovar el access token usando un refresh token válido, sin requerir que el usuario ingrese sus credenciales nuevamente. SHALL implementar refresh token rotation: cada uso genera un nuevo refresh token e invalida el anterior.

#### Scenario: Refresh exitoso
- **WHEN** el cliente envía un refresh token válido, no vencido y no revocado
- **THEN** el sistema retorna `200 OK` con nuevo `accessToken` y nuevo `refreshToken`, y revoca el refresh token anterior

#### Scenario: Refresh token vencido
- **WHEN** el cliente envía un refresh token cuyo `expires_at` ya pasó
- **THEN** el sistema retorna `401 Unauthorized` con código `REFRESH_EXPIRED`

#### Scenario: Refresh token ya usado (comprometido)
- **WHEN** el cliente envía un refresh token que ya fue rotado (tiene `revoked_at`)
- **THEN** el sistema invalida TODOS los refresh tokens del usuario, retorna `401 Unauthorized` con código `REFRESH_EXPIRED`

---

### Requirement: Logout y revocación de sesión
El sistema SHALL permitir al usuario cerrar sesión revocando el refresh token activo. SHALL también permitir listar y revocar sesiones activas individuales.

#### Scenario: Logout exitoso
- **WHEN** el usuario autenticado llama a `POST /v1/auth/logout` con su refresh token
- **THEN** el sistema revoca el refresh token (setea `revoked_at`) y retorna `204 No Content`

#### Scenario: Listar sesiones activas
- **WHEN** el usuario autenticado llama a `GET /v1/auth/me/sessions`
- **THEN** el sistema retorna la lista de refresh tokens activos del usuario con `ip_address`, `user_agent` y `created_at`

#### Scenario: Revocar sesión específica
- **WHEN** el usuario autenticado llama a `DELETE /v1/auth/me/sessions/{id}` con el ID de una sesión que le pertenece
- **THEN** el sistema revoca esa sesión y retorna `204 No Content`

---

### Requirement: Cambio de contraseña autenticado
El sistema SHALL permitir al usuario autenticado cambiar su contraseña proporcionando la contraseña actual y la nueva.

#### Scenario: Cambio exitoso
- **WHEN** el usuario autenticado envía `currentPassword` correcta y `newPassword` que cumple las reglas mínimas (8+ caracteres)
- **THEN** el sistema actualiza el `password_hash`, revoca todos los refresh tokens del usuario excepto el actual, y retorna `204 No Content`

#### Scenario: Contraseña actual incorrecta
- **WHEN** el usuario envía `currentPassword` que no coincide con el hash almacenado
- **THEN** el sistema retorna `400 Bad Request` con código `INVALID_CURRENT_PASSWORD`

---

### Requirement: Reset de contraseña por email
El sistema SHALL permitir solicitar un reset de contraseña por email cuando el usuario no recuerda su contraseña. SHALL generar un token de un solo uso con expiración de 2 horas.

#### Scenario: Solicitud de reset enviada
- **WHEN** el usuario envía `POST /v1/auth/forgot-password` con email y tenantSlug válidos
- **THEN** el sistema genera un token de reset, lo almacena en el usuario y retorna `200 OK` (sin indicar si el email existe, por seguridad)

#### Scenario: Reset exitoso
- **WHEN** el usuario envía `POST /v1/auth/reset-password` con token válido y no expirado, y nueva contraseña válida
- **THEN** el sistema actualiza el `password_hash`, invalida el token de reset, revoca todos los refresh tokens del usuario, y retorna `204 No Content`

#### Scenario: Token de reset expirado
- **WHEN** el usuario envía un token de reset cuyo `password_reset_expires_at` ya pasó
- **THEN** el sistema retorna `400 Bad Request` con código `RESET_TOKEN_EXPIRED`
