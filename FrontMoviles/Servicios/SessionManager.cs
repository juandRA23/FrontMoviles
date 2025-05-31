using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Servicios/SessionManager.cs
using FrontMoviles.Modelos;

namespace FrontMoviles.Servicios
{
    public static class SessionManager
    {
        private const string SESSION_ID_KEY = "SessionId";
        private const string TOKEN_KEY = "Token";
        private const string IS_LOGGED_IN_KEY = "IsLoggedIn";
        private const string USER_EMAIL_KEY = "UserEmail";

        public static void GuardarSesion(Sesion sesion, string userEmail)
        {
            Preferences.Set(SESSION_ID_KEY, sesion.SesionId);
            Preferences.Set(TOKEN_KEY, sesion.Token);
            Preferences.Set(IS_LOGGED_IN_KEY, true);
            Preferences.Set(USER_EMAIL_KEY, userEmail);
        }

        public static void CerrarSesion()
        {
            Preferences.Remove(SESSION_ID_KEY);
            Preferences.Remove(TOKEN_KEY);
            Preferences.Remove(IS_LOGGED_IN_KEY);
            Preferences.Remove(USER_EMAIL_KEY);
        }

        public static bool EstaLogueado()
        {
            return Preferences.Get(IS_LOGGED_IN_KEY, false);
        }

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

        public static void ActualizarToken(string nuevoToken)
        {
            Preferences.Set(TOKEN_KEY, nuevoToken);
        }
    }
}