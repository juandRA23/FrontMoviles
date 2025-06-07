using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class CrearResenaPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Servicio _servicio;
    private int _calificacionSeleccionada = 0;
    private List<Label> _estrellas;

    public CrearResenaPage(Servicio servicio)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
        _estrellas = new List<Label> { Star1, Star2, Star3, Star4, Star5 };

        CargarDatosServicio();
    }

    #region Configuración inicial

    private void CargarDatosServicio()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🌟 Creando reseña para servicio: {_servicio.Titulo}");

            // Información del servicio
            CategoriaServicioLabel.Text = _servicio.Categoria?.Nombre?.ToUpper() ?? "SERVICIO";
            TituloServicioLabel.Text = _servicio.Titulo;

            // Proveedor
            if (_servicio.Usuario != null)
            {
                var nombreCompleto = $"{_servicio.Usuario.Nombre} {_servicio.Usuario.Apellido1}";
                ProveedorServicioLabel.Text = $"Por: {nombreCompleto}";
            }
            else
            {
                ProveedorServicioLabel.Text = "Por: Proveedor";
            }

            // Configurar icono y color según categoría
            ConfigurarIconoCategoria();

            System.Diagnostics.Debug.WriteLine("✅ Datos del servicio cargados");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos del servicio: {ex.Message}");
            DisplayAlert("Error", $"Error al cargar información del servicio: {ex.Message}", "OK");
        }
    }

    private void ConfigurarIconoCategoria()
    {
        try
        {
            var categoria = _servicio.Categoria?.Nombre?.ToLower();

            var (icono, color) = categoria switch
            {
                "educación" or "educacion" => ("📚", "#6C7CE7"),
                "tecnología" or "tecnologia" => ("💻", "#34C759"),
                "hogar" => ("🏠", "#FF9500"),
                "diseño" or "diseno" => ("🎨", "#FF2D92"),
                "salud" => ("⚕️", "#00C7BE"),
                "transporte" => ("🚗", "#FF6B35"),
                "belleza" => ("💄", "#E91E63"),
                "deportes" => ("⚽", "#4CAF50"),
                _ => ("🔧", "#A8D5BA")
            };

            IconoServicioLabel.Text = icono;
            IconoServicioFrame.BackgroundColor = Color.FromArgb(color);

            System.Diagnostics.Debug.WriteLine($"✅ Icono configurado: {icono} con color {color}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error configurando icono: {ex.Message}");
            IconoServicioLabel.Text = "🔧";
            IconoServicioFrame.BackgroundColor = Color.FromArgb("#A8D5BA");
        }
    }

    #endregion

    #region Eventos de UI

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (TieneContenido())
        {
            bool salir = await DisplayAlert(
                "Descartar reseña",
                "¿Estás seguro que deseas salir? Se perderá tu reseña.",
                "Sí, salir",
                "Continuar escribiendo");

            if (!salir) return;
        }

        await Navigation.PopAsync();
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Ayuda - Escribir Reseña",
            "• Califica el servicio del 1 al 5 estrellas\n" +
            "• 1 estrella = Muy malo\n" +
            "• 5 estrellas = Excelente\n" +
            "• El comentario es opcional pero ayuda mucho\n" +
            "• Sé honesto y constructivo en tu reseña\n" +
            "• Las reseñas ayudan a otros usuarios", "OK");
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        if (TieneContenido())
        {
            bool confirmar = await DisplayAlert(
                "Cancelar reseña",
                "¿Estás seguro que deseas cancelar? Se perderá tu reseña.",
                "Sí, cancelar",
                "Continuar escribiendo");

            if (!confirmar) return;
        }

        await Navigation.PopAsync();
    }

    #endregion

    #region Manejo de calificación (estrellas)

    private void OnStarClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is Label star && star.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap)
            {
                if (int.TryParse(tap.CommandParameter?.ToString(), out int calificacion))
                {
                    _calificacionSeleccionada = calificacion;
                    ActualizarEstrellas();
                    ActualizarTextoCalificacion();
                    ValidarFormulario();

                    System.Diagnostics.Debug.WriteLine($"⭐ Calificación seleccionada: {_calificacionSeleccionada}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en selección de estrella: {ex.Message}");
        }
    }

    private void ActualizarEstrellas()
    {
        try
        {
            for (int i = 0; i < _estrellas.Count; i++)
            {
                if (i < _calificacionSeleccionada)
                {
                    _estrellas[i].Text = "★";
                    _estrellas[i].TextColor = Color.FromArgb("#FFD700"); // Dorado
                }
                else
                {
                    _estrellas[i].Text = "☆";
                    _estrellas[i].TextColor = Color.FromArgb("#E0E0E0"); // Gris
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error actualizando estrellas: {ex.Message}");
        }
    }

    private void ActualizarTextoCalificacion()
    {
        try
        {
            CalificacionTextoLabel.Text = _calificacionSeleccionada switch
            {
                1 => "⭐ Muy malo",
                2 => "⭐⭐ Malo",
                3 => "⭐⭐⭐ Regular",
                4 => "⭐⭐⭐⭐ Bueno",
                5 => "⭐⭐⭐⭐⭐ Excelente",
                _ => "Toca las estrellas para calificar"
            };

            CalificacionTextoLabel.TextColor = _calificacionSeleccionada switch
            {
                1 or 2 => Color.FromArgb("#FF4444"),
                3 => Color.FromArgb("#FF8800"),
                4 or 5 => Color.FromArgb("#4CAF50"),
                _ => Colors.Gray
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error actualizando texto calificación: {ex.Message}");
        }
    }

    #endregion

    #region Eventos del comentario

    private void OnComentarioTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var texto = e.NewTextValue ?? "";
            var caracteres = texto.Length;

            ContadorCaracteresLabel.Text = $"{caracteres}/500 caracteres";

            // Cambiar color si se acerca al límite
            if (caracteres > 450)
            {
                ContadorCaracteresLabel.TextColor = Color.FromArgb("#FF4444");
            }
            else if (caracteres > 400)
            {
                ContadorCaracteresLabel.TextColor = Color.FromArgb("#FF8800");
            }
            else
            {
                ContadorCaracteresLabel.TextColor = Colors.Gray;
            }

            ValidarFormulario();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en cambio de comentario: {ex.Message}");
        }
    }

    #endregion

    #region Validaciones

    private void ValidarFormulario()
    {
        try
        {
            // La reseña es válida si tiene al menos una calificación
            bool esValido = _calificacionSeleccionada > 0;

            PublicarButton.IsEnabled = esValido;

            if (esValido)
            {
                PublicarButton.BackgroundColor = Color.FromArgb("#4A7C59");
            }
            else
            {
                PublicarButton.BackgroundColor = Color.FromArgb("#CCCCCC");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error validando formulario: {ex.Message}");
        }
    }

    private bool TieneContenido()
    {
        return _calificacionSeleccionada > 0 || !string.IsNullOrWhiteSpace(ComentarioEditor.Text);
    }

    #endregion

    #region Publicar reseña

    private async void OnPublicarClicked(object sender, EventArgs e)
    {
        if (_calificacionSeleccionada == 0)
        {
            await DisplayAlert("Calificación requerida", "Por favor selecciona una calificación con las estrellas", "OK");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine("🌟 === PUBLICANDO RESEÑA ===");

            // Mostrar indicador de carga
            var button = sender as Button;
            var originalText = button?.Text;
            if (button != null)
            {
                button.Text = "Publicando...";
                button.IsEnabled = false;
            }

            // Verificar sesión
            if (!SessionManager.EstaLogueado())
            {
                await MostrarErrorSesionYRedirigir();
                return;
            }

            // Crear la reseña
            var resena = new Resena
            {
                ResenaId = 0, // Nuevo
                Servicio = _servicio,
                Usuario = null, // Se asignará en el servidor
                Calificacion = _calificacionSeleccionada,
                Comentario = ComentarioEditor.Text?.Trim() ?? "",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Crear request
            var request = new ReqInsertarResena
            {
                SesionId = SessionManager.ObtenerSessionId(),
                Resena = resena
            };

            System.Diagnostics.Debug.WriteLine($"📤 Enviando reseña:");
            System.Diagnostics.Debug.WriteLine($"  - Servicio: {_servicio.Titulo} (ID: {_servicio.ServicioId})");
            System.Diagnostics.Debug.WriteLine($"  - Calificación: {_calificacionSeleccionada}");
            System.Diagnostics.Debug.WriteLine($"  - Comentario: {resena.Comentario}");

            // Llamar al API
            var response = await _apiService.InsertarResenaAsync(request);

            if (response.Resultado)
            {
                System.Diagnostics.Debug.WriteLine("✅ ÉXITO - Reseña publicada");

                await DisplayAlert("¡Éxito!", "Tu reseña ha sido publicada exitosamente. ¡Gracias por tu opinión!", "OK");

                // Regresar a la página anterior
                await Navigation.PopAsync();
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                System.Diagnostics.Debug.WriteLine($"❌ ERROR API: {errorMessage}");

                if (EsErrorDeSesion(errorMessage))
                {
                    await MostrarErrorSesionYRedirigir();
                    return;
                }

                await DisplayAlert("Error", $"No se pudo publicar la reseña:\n{errorMessage}", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 EXCEPCIÓN: {ex.Message}");
            await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar botón
            if (sender is Button btn)
            {
                btn.Text = "Publicar Reseña";
                btn.IsEnabled = _calificacionSeleccionada > 0;
            }

            System.Diagnostics.Debug.WriteLine("🏁 FIN DE PUBLICAR RESEÑA");
        }
    }

    private static bool EsErrorDeSesion(string errorMessage)
    {
        var mensaje = errorMessage.ToLower();
        return mensaje.Contains("sesion") ||
               mensaje.Contains("token") ||
               mensaje.Contains("unauthorized") ||
               mensaje.Contains("authentication") ||
               mensaje.Contains("forbidden");
    }

    private async Task MostrarErrorSesionYRedirigir()
    {
        try
        {
            SessionManager.CerrarSesion();
            await DisplayAlert("Sesión Expirada", "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.", "OK");
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error redirigiendo: {ex.Message}");
        }
    }

    #endregion

    #region Lifecycle

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _apiService?.Dispose();
        System.Diagnostics.Debug.WriteLine("🚪 CrearResenaPage cerrada");
    }

    #endregion
}