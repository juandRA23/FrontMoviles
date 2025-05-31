using System;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using FrontMoviles.Modelos;

namespace FrontMoviles.Servicios
{
    public static class SessionManager
    {
        private const string SESSION_ID_KEY = "SessionId";
        private const string TOKEN_KEY = "Token";
        private const string IS_LOGGED_IN_KEY = "IsLoggedIn";
        private const string USER_EMAIL_KEY = "UserEmail";
        private const string USER_ID_KEY = "UserId";
        private const string USER_NAME_KEY = "UserName";
        private const string TOKEN_EXPIRY_KEY = "TokenExpiry";
        private const string REFRESH_TOKEN_KEY = "RefreshToken";
        private const string SESSION_DATA_KEY = "SessionData";

        #region Métodos principales de sesión

        public static void GuardarSesion(Sesion sesion, string userEmail, string userName = null, int userId = 0)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("💾 === GUARDANDO SESIÓN ===");

                // Guardar datos básicos de sesión
                Preferences.Set(SESSION_ID_KEY, sesion.SesionId ?? string.Empty);
                Preferences.Set(TOKEN_KEY, sesion.Token ?? string.Empty);
                Preferences.Set(IS_LOGGED_IN_KEY, true);
                Preferences.Set(USER_EMAIL_KEY, userEmail ?? string.Empty);
                Preferences.Set(USER_ID_KEY, userId);
                Preferences.Set(USER_NAME_KEY, userName ?? string.Empty);

                System.Diagnostics.Debug.WriteLine($"📱 SessionId guardado: {sesion.SesionId}");
                System.Diagnostics.Debug.WriteLine($"🔑 Token guardado: {(!string.IsNullOrEmpty(sesion.Token) ? "SÍ" : "NO")}");
                System.Diagnostics.Debug.WriteLine($"📧 Email guardado: {userEmail}");

                // Extraer y guardar información del JWT
                if (!string.IsNullOrEmpty(sesion.Token))
                {
                    var tokenInfo = ExtraerInformacionJWT(sesion.Token);
                    if (tokenInfo.Expiry.HasValue)
                    {
                        Preferences.Set(TOKEN_EXPIRY_KEY, tokenInfo.Expiry.Value.ToBinary());
                        System.Diagnostics.Debug.WriteLine($"⏰ Expiración JWT guardada: {tokenInfo.Expiry.Value:yyyy-MM-dd HH:mm:ss} UTC");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ No se pudo extraer fecha de expiración del JWT");
                    }
                }

                // Guardar fechas de sesión
                if (!string.IsNullOrEmpty(sesion.FechaCreacion))
                {
                    if (DateTime.TryParse(sesion.FechaCreacion, out DateTime fechaCreacion))
                    {
                        Preferences.Set("SessionCreatedAt", fechaCreacion.ToBinary());
                        System.Diagnostics.Debug.WriteLine($"📅 Fecha creación guardada: {fechaCreacion:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                // Serializar y guardar toda la sesión para respaldo
                var sessionJson = JsonSerializer.Serialize(sesion);
                Preferences.Set(SESSION_DATA_KEY, sessionJson);

                System.Diagnostics.Debug.WriteLine($"✅ Sesión guardada para: {userEmail}");
                System.Diagnostics.Debug.WriteLine("==========================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al guardar sesión: {ex.Message}");
            }
        }

        public static void CerrarSesion()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚪 === CERRANDO SESIÓN ===");

                // Limpiar todas las preferencias de sesión
                Preferences.Remove(SESSION_ID_KEY);
                Preferences.Remove(TOKEN_KEY);
                Preferences.Remove(IS_LOGGED_IN_KEY);
                Preferences.Remove(USER_EMAIL_KEY);
                Preferences.Remove(USER_ID_KEY);
                Preferences.Remove(USER_NAME_KEY);
                Preferences.Remove(TOKEN_EXPIRY_KEY);
                Preferences.Remove(REFRESH_TOKEN_KEY);
                Preferences.Remove(SESSION_DATA_KEY);
                Preferences.Remove("SessionCreatedAt");

                System.Diagnostics.Debug.WriteLine("✅ Sesión cerrada correctamente");
                System.Diagnostics.Debug.WriteLine("==========================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al cerrar sesión: {ex.Message}");
            }
        }

        #endregion

        #region Métodos de verificación de sesión

        public static bool EstaLogueado()
        {
            try
            {
                var isLoggedIn = Preferences.Get(IS_LOGGED_IN_KEY, false);
                var hasToken = !string.IsNullOrEmpty(ObtenerToken());
                var hasSessionId = !string.IsNullOrEmpty(ObtenerSessionId());
                var tokenExpirado = TokenExpirado();

                System.Diagnostics.Debug.WriteLine($"🔍 Verificando si está logueado:");
                System.Diagnostics.Debug.WriteLine($"  - IsLoggedIn flag: {isLoggedIn}");
                System.Diagnostics.Debug.WriteLine($"  - Tiene token: {hasToken}");
                System.Diagnostics.Debug.WriteLine($"  - Tiene SessionId: {hasSessionId}");
                System.Diagnostics.Debug.WriteLine($"  - Token expirado: {tokenExpirado}");

                var resultado = isLoggedIn && hasToken && hasSessionId && !tokenExpirado;
                System.Diagnostics.Debug.WriteLine($"  - RESULTADO: {resultado}");

                return resultado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error verificando sesión: {ex.Message}");
                return false;
            }
        }

        public static bool TokenExpirado()
        {
            try
            {
                var token = ObtenerToken();
                if (string.IsNullOrEmpty(token))
                {
                    System.Diagnostics.Debug.WriteLine("❌ No hay token para verificar");
                    return true;
                }

                // DIAGNÓSTICO DETALLADO
                System.Diagnostics.Debug.WriteLine("🔍 === DIAGNÓSTICO TOKEN EXPIRADO ===");

                var ahora = DateTime.UtcNow;
                var ahoraLocal = DateTime.Now;

                System.Diagnostics.Debug.WriteLine($"🕐 Hora actual UTC: {ahora:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"🕐 Hora actual Local: {ahoraLocal:yyyy-MM-dd HH:mm:ss}");
                System.Diagnostics.Debug.WriteLine($"🌍 Zona horaria: {TimeZoneInfo.Local.DisplayName}");
                System.Diagnostics.Debug.WriteLine($"🌍 Offset UTC: {TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)}");

                // Verificar expiración desde JWT
                var tokenInfo = ExtraerInformacionJWT(token);
                if (tokenInfo.Expiry.HasValue)
                {
                    var expiry = tokenInfo.Expiry.Value;
                    var expiryLocal = expiry.ToLocalTime();

                    System.Diagnostics.Debug.WriteLine($"⏰ Token expira UTC: {expiry:yyyy-MM-dd HH:mm:ss}");
                    System.Diagnostics.Debug.WriteLine($"⏰ Token expira Local: {expiryLocal:yyyy-MM-dd HH:mm:ss}");

                    var diferencia = expiry - ahora;
                    var diferenciaMinutos = diferencia.TotalMinutes;

                    System.Diagnostics.Debug.WriteLine($"📏 Diferencia de tiempo: {diferencia}");
                    System.Diagnostics.Debug.WriteLine($"📏 Diferencia en minutos: {diferenciaMinutos:F2}");

                    if (diferenciaMinutos > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Token válido por {diferenciaMinutos:F1} minutos más");
                        System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO (Token VÁLIDO) ===");
                        return false; // Token NO expirado
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Token expiró hace {Math.Abs(diferenciaMinutos):F1} minutos");
                        System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO (Token EXPIRADO) ===");
                        return true; // Token expirado
                    }
                }

                // Verificar desde preferences como respaldo
                var expiryBinary = Preferences.Get(TOKEN_EXPIRY_KEY, 0L);
                if (expiryBinary != 0)
                {
                    var expiry = DateTime.FromBinary(expiryBinary);
                    System.Diagnostics.Debug.WriteLine($"⏰ Expiry desde Preferences: {expiry:yyyy-MM-dd HH:mm:ss}");

                    var diferencia = expiry - ahora;
                    System.Diagnostics.Debug.WriteLine($"📏 Diferencia Preferences: {diferencia}");

                    var resultado = DateTime.UtcNow >= expiry;
                    System.Diagnostics.Debug.WriteLine($"📊 Resultado desde Preferences: {(resultado ? "EXPIRADO" : "VÁLIDO")}");
                    System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO (Preferences) ===");
                    return resultado;
                }

                System.Diagnostics.Debug.WriteLine("❌ No se pudo determinar expiración - considerando expirado");
                System.Diagnostics.Debug.WriteLine("=== FIN DIAGNÓSTICO (Sin info) ===");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Error verificando expiración: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                return true; // En caso de error, considerar expirado
            }
        }

        public static bool SesionExpirada()
        {
            var logueado = EstaLogueado();
            var tokenExpirado = TokenExpirado();

            System.Diagnostics.Debug.WriteLine($"🔍 SesionExpirada - Logueado: {logueado}, Token expirado: {tokenExpirado}");

            return !logueado || tokenExpirado;
        }

        #endregion

        #region Métodos de obtención de datos

        public static string ObtenerSessionId()
        {
            return Preferences.Get(SESSION_ID_KEY, string.Empty);
        }

        public static string ObtenerToken()
        {
            return Preferences.Get(TOKEN_KEY, string.Empty);
        }

        public static string ObtenerEmailUsuario()
        {
            return Preferences.Get(USER_EMAIL_KEY, string.Empty);
        }

        public static string ObtenerNombreUsuario()
        {
            return Preferences.Get(USER_NAME_KEY, string.Empty);
        }

        public static int ObtenerIdUsuario()
        {
            return Preferences.Get(USER_ID_KEY, 0);
        }

        public static DateTime? ObtenerFechaExpiracion()
        {
            try
            {
                var expiryBinary = Preferences.Get(TOKEN_EXPIRY_KEY, 0L);
                if (expiryBinary != 0)
                {
                    return DateTime.FromBinary(expiryBinary);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static Sesion ObtenerSesionCompleta()
        {
            try
            {
                var sessionJson = Preferences.Get(SESSION_DATA_KEY, string.Empty);
                if (!string.IsNullOrEmpty(sessionJson))
                {
                    return JsonSerializer.Deserialize<Sesion>(sessionJson);
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo sesión completa: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos de actualización

        public static void ActualizarToken(string nuevoToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Actualizando token...");
                Preferences.Set(TOKEN_KEY, nuevoToken);

                // Actualizar información de expiración
                if (!string.IsNullOrEmpty(nuevoToken))
                {
                    var tokenInfo = ExtraerInformacionJWT(nuevoToken);
                    if (tokenInfo.Expiry.HasValue)
                    {
                        Preferences.Set(TOKEN_EXPIRY_KEY, tokenInfo.Expiry.Value.ToBinary());
                        System.Diagnostics.Debug.WriteLine($"⏰ Nueva expiración: {tokenInfo.Expiry.Value:yyyy-MM-dd HH:mm:ss} UTC");
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ Token actualizado correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando token: {ex.Message}");
            }
        }

        public static void ActualizarDatosUsuario(string nombre, int userId)
        {
            try
            {
                if (!string.IsNullOrEmpty(nombre))
                {
                    Preferences.Set(USER_NAME_KEY, nombre);
                }

                if (userId > 0)
                {
                    Preferences.Set(USER_ID_KEY, userId);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Datos de usuario actualizados: {nombre}, ID: {userId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error actualizando datos de usuario: {ex.Message}");
            }
        }

        #endregion

        #region Métodos auxiliares para JWT

        private static (DateTime? Expiry, string UserId, string Email) ExtraerInformacionJWT(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                // Extraer fecha de expiración
                DateTime? expiry = null;
                if (jsonToken.ValidTo != DateTime.MinValue)
                {
                    expiry = jsonToken.ValidTo;
                    System.Diagnostics.Debug.WriteLine($"🔍 JWT ValidTo: {jsonToken.ValidTo:yyyy-MM-dd HH:mm:ss} (Kind: {jsonToken.ValidTo.Kind})");
                }

                // Extraer claims comunes
                var userId = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "userId")?.Value;
                var email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "emailaddress")?.Value;

                System.Diagnostics.Debug.WriteLine($"🔍 JWT Claims - UserId: {userId}, Email: {email}");

                return (expiry, userId, email);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error extrayendo información del JWT: {ex.Message}");
                return (null, null, null);
            }
        }

        public static string ObtenerClaimDelToken(string claimType)
        {
            try
            {
                var token = ObtenerToken();
                if (string.IsNullOrEmpty(token))
                    return string.Empty;

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                return jsonToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error obteniendo claim {claimType}: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region Métodos de depuración y utilidad

        public static void ImprimirInformacionSesion()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== INFORMACIÓN DE SESIÓN ===");
                System.Diagnostics.Debug.WriteLine($"Logueado: {EstaLogueado()}");
                System.Diagnostics.Debug.WriteLine($"Token expirado: {TokenExpirado()}");
                System.Diagnostics.Debug.WriteLine($"Email: {ObtenerEmailUsuario()}");
                System.Diagnostics.Debug.WriteLine($"Nombre: {ObtenerNombreUsuario()}");
                System.Diagnostics.Debug.WriteLine($"Session ID: {ObtenerSessionId()}");
                System.Diagnostics.Debug.WriteLine($"User ID: {ObtenerIdUsuario()}");

                var expiry = ObtenerFechaExpiracion();
                if (expiry.HasValue)
                {
                    var ahora = DateTime.UtcNow;
                    var tiempoRestante = expiry.Value.Subtract(ahora);

                    System.Diagnostics.Debug.WriteLine($"Expira: {expiry.Value:yyyy-MM-dd HH:mm:ss} UTC");

                    if (tiempoRestante.TotalSeconds > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Tiempo restante: {tiempoRestante:hh\\:mm\\:ss}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Expiró hace: {tiempoRestante.Negate():hh\\:mm\\:ss}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Expira: No se pudo determinar");
                }
                System.Diagnostics.Debug.WriteLine("===========================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error imprimiendo información de sesión: {ex.Message}");
            }
        }

        public static void LimpiarSesionExpirada()
        {
            try
            {
                if (TokenExpirado())
                {
                    System.Diagnostics.Debug.WriteLine("🧹 Limpiando sesión expirada automáticamente");
                    CerrarSesion();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error limpiando sesión expirada: {ex.Message}");
            }
        }

        #endregion
    }
}