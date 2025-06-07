using FrontMoviles.Servicios;
using FrontMoviles.Modelos;
using System.Globalization;

namespace FrontMoviles;

public partial class PerfilPage : ContentPage
{
    private readonly ApiService _apiService;
    private Usuario _usuarioActual;

    public PerfilPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        CargarPerfilUsuario();
    }

    #region Carga de datos

    private async void CargarPerfilUsuario()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 === CARGANDO PERFIL CON NUEVA API ===");

            // Mostrar indicador de carga
            MostrarEstado("loading");

            // Verificar sesión antes de cargar
            if (!SessionManager.EstaLogueado())
            {
                System.Diagnostics.Debug.WriteLine("❌ No hay sesión activa en PerfilPage");
                MostrarError("No hay sesión activa. Por favor, inicia sesión.");
                return;
            }

            var userEmail = SessionManager.ObtenerEmailUsuario();
            var sesionId = SessionManager.ObtenerSessionId();

            System.Diagnostics.Debug.WriteLine($"✅ Usuario logueado: {userEmail}");
            System.Diagnostics.Debug.WriteLine($"✅ SesionId: {sesionId}");

            // Llamar a la nueva API usando SesionId
            var response = await _apiService.ObtenerPerfilUsuarioAsync();

            if (response.Resultado && response.Usuario != null)
            {
                _usuarioActual = response.Usuario;

                System.Diagnostics.Debug.WriteLine($"✅ Perfil cargado: {_usuarioActual.Nombre} {_usuarioActual.Apellido1}");

                CargarDatosEnUI(_usuarioActual);
                MostrarEstado("content");
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando perfil: {errorMessage}");

                // Si es error de sesión, redirigir al login
                if (errorMessage.Contains("sesión") || errorMessage.Contains("inválida") || errorMessage.Contains("inicia sesión"))
                {
                    await MostrarErrorSesionYRedirigir();
                    return;
                }

                MostrarError($"Error al cargar perfil: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Excepción en CargarPerfilUsuario: {ex.Message}");
            MostrarError($"Error inesperado: {ex.Message}");
        }
    }

    private async Task MostrarErrorSesionYRedirigir()
    {
        try
        {
            SessionManager.CerrarSesion();

            await DisplayAlert("Sesión Expirada",
                "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.", "OK");

            // Redirigir al login
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error redirigiendo: {ex.Message}");
        }
    }

    private void CargarDatosEnUI(Usuario usuario)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🎨 Cargando datos en UI...");

            // Nombre completo
            var nombreCompleto = $"{usuario.Nombre} {usuario.Apellido1}";
            if (!string.IsNullOrEmpty(usuario.Apellido2))
            {
                nombreCompleto += $" {usuario.Apellido2}";
            }
            NombreCompletoLabel.Text = nombreCompleto;

            // Email
            EmailLabel.Text = usuario.Correo;

            // Teléfono
            TelefonoLabel.Text = FormatearTelefono(usuario.Telefono);

            // Fecha de nacimiento
            if (DateTime.TryParse(usuario.FechaNacimiento.ToString(), out DateTime fechaNacimiento))
            {
                FechaNacimientoLabel.Text = fechaNacimiento.ToString("dd 'de' MMMM, yyyy", new CultureInfo("es-ES"));
            }
            else
            {
                FechaNacimientoLabel.Text = "No especificado";
            }

            // Ubicación
            var ubicacion = "";
            if (usuario.Canton != null && usuario.Canton.Provincia != null)
            {
                ubicacion = $"{usuario.Canton.Nombre}, {usuario.Canton.Provincia.Nombre}";
            }
            else if (usuario.Provincia != null)
            {
                ubicacion = usuario.Provincia.Nombre;
            }
            else
            {
                ubicacion = "No especificado";
            }
            UbicacionLabel.Text = ubicacion;

            // Dirección exacta
            DireccionLabel.Text = string.IsNullOrEmpty(usuario.Direccion) ? "No especificado" : usuario.Direccion;
            DireccionLabel.IsVisible = !string.IsNullOrEmpty(usuario.Direccion);

            // Estado de verificación
            ActualizarEstadoVerificacion(usuario.Verificacion > 0);

            // Foto de perfil
            if (!string.IsNullOrEmpty(usuario.FotoPerfil))
            {
                // Aquí podrías cargar la imagen real si tienes URLs de imágenes
                FotoPerfilLabel.Text = "📷";
            }

            System.Diagnostics.Debug.WriteLine("✅ Datos cargados en UI correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos en UI: {ex.Message}");
            DisplayAlert("Error", $"Error al mostrar datos: {ex.Message}", "OK");
        }
    }

    private void ActualizarEstadoVerificacion(bool verificado)
    {
        if (verificado)
        {
            VerificacionFrame.BackgroundColor = Color.FromArgb("#28a745"); // Verde
            VerificacionLabel.Text = "✓ Cuenta verificada";
        }
        else
        {
            VerificacionFrame.BackgroundColor = Color.FromArgb("#ffc107"); // Amarillo
            VerificacionLabel.Text = "⚠ Cuenta no verificada";
        }
    }

    private string FormatearTelefono(string telefono)
    {
        if (string.IsNullOrEmpty(telefono))
            return "No especificado";

        // Remover espacios y caracteres especiales
        var numeroLimpio = telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        // Si es un número de Costa Rica (8 dígitos que empiezan con 6, 7 u 8)
        if (numeroLimpio.Length == 8 && (numeroLimpio.StartsWith("6") || numeroLimpio.StartsWith("7") || numeroLimpio.StartsWith("8")))
        {
            return $"+506 {numeroLimpio.Substring(0, 4)}-{numeroLimpio.Substring(4)}";
        }

        // Si ya tiene código de país
        if (numeroLimpio.StartsWith("506") && numeroLimpio.Length == 11)
        {
            return $"+506 {numeroLimpio.Substring(3, 4)}-{numeroLimpio.Substring(7)}";
        }

        return telefono; // Devolver como está si no se puede formatear
    }

    private void MostrarEstado(string estado)
    {
        LoadingGrid.IsVisible = estado == "loading";
        ContentScrollView.IsVisible = estado == "content";
        ErrorGrid.IsVisible = estado == "error";
        System.Diagnostics.Debug.WriteLine($"🔄 Estado UI cambiado a: {estado}");
    }

    private void MostrarError(string mensaje)
    {
        ErrorMessageLabel.Text = mensaje;
        MostrarEstado("error");
        System.Diagnostics.Debug.WriteLine($"❌ Error mostrado: {mensaje}");
    }

    #endregion

    #region Eventos de navegación

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnConfigClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Configuración", "Función en desarrollo", "OK");
    }

    private async void OnReintentarClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔄 Reintentando cargar perfil...");
        CargarPerfilUsuario();
    }

    #endregion

    #region Eventos de opciones

    private async void OnEditarPerfilClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("✏️ Navegando a editar perfil...");

            // Verificar sesión antes de navegar
            if (!SessionManager.EstaLogueado())
            {
                await DisplayAlert("Sesión requerida", "Debes iniciar sesión para editar el perfil", "OK");
                return;
            }

            // Navegar a la página de editar perfil
            await Navigation.PushAsync(new EditarPerfilPage());

            System.Diagnostics.Debug.WriteLine("✅ Navegación a EditarPerfilPage exitosa");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error navegando a editar perfil: {ex.Message}");
            await DisplayAlert("Error", $"Error al abrir edición de perfil: {ex.Message}", "OK");
        }
    }
    private async void OnCambiarContrasenaClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Cambiar Contraseña", "Función en desarrollo", "OK");
        // Aquí navegarías a una página para cambiar contraseña
        // await Navigation.PushAsync(new CambiarContrasenaPage());
    }

    private async void OnCerrarSesionClicked(object sender, EventArgs e)
    {
        try
        {
            bool confirmar = await DisplayAlert(
                "Cerrar Sesión",
                "¿Estás seguro que deseas cerrar sesión?",
                "Sí",
                "Cancelar");

            if (confirmar)
            {
                System.Diagnostics.Debug.WriteLine("🚪 Cerrando sesión desde perfil...");

                // Limpiar la sesión
                SessionManager.CerrarSesion();

                // Navegar a la página principal
                Application.Current.MainPage = new AppShell();

                // Mostrar mensaje de confirmación
                await DisplayAlert("Sesión Cerrada", "Has cerrado sesión exitosamente", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cerrando sesión: {ex.Message}");
            await DisplayAlert("Error", $"Error al cerrar sesión: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Cleanup

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _apiService?.Dispose();
        System.Diagnostics.Debug.WriteLine("🚪 PerfilPage cerrada");
    }

    #endregion
}