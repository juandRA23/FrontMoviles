// FrontMoviles/DetalleServicioPage.xaml.cs - VERSIÓN CORREGIDA
using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class DetalleServicioPage : ContentPage
{
    private readonly Servicio _servicio;
    private readonly ApiService _apiService;
    private List<Resena> _resenasDelServicio = new List<Resena>();

    public DetalleServicioPage(Servicio servicio)
    {
        InitializeComponent();
        _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));
        _apiService = new ApiService();

        CargarDatosServicio();
        CargarResenasReales();
    }

    #region Carga de datos

    private void CargarDatosServicio()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"📄 Cargando detalle del servicio: {_servicio.Titulo}");

            // Cargar información básica
            CargarInformacionBasica();

            // Cargar información del proveedor
            CargarInformacionProveedor();

            // Cargar subcategorías
            CargarSubcategorias();

            // Cargar información adicional
            CargarInformacionAdicional();

            System.Diagnostics.Debug.WriteLine("✅ Detalle del servicio cargado correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando detalle: {ex.Message}");
            DisplayAlert("Error", $"Error al cargar información: {ex.Message}", "OK");
        }
    }

    private void CargarInformacionBasica()
    {
        try
        {
            // Título y categoría
            TituloLabel.Text = _servicio.Titulo;
            CategoriaLabel.Text = _servicio.Categoria?.Nombre?.ToUpper() ?? "SERVICIO";

            // Descripción
            DescripcionLabel.Text = _servicio.Descripcion;

            // Precio
            PrecioLabel.Text = $"₡{_servicio.Precio:N0}";

            // Disponibilidad
            DisponibilidadLabel.Text = _servicio.Disponibilidad;

            // Configurar icono y color según categoría
            ConfigurarIconoCategoria();

            // Calificación (simulada por ahora)
            CalificacionLabel.Text = "4.8";
            ResenasLabel.Text = "(15 reseñas)";

            System.Diagnostics.Debug.WriteLine("✅ Información básica cargada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en información básica: {ex.Message}");
        }
    }

    private void CargarInformacionProveedor()
    {
        try
        {
            if (_servicio.Usuario != null)
            {
                // Nombre completo del proveedor
                var nombreCompleto = $"{_servicio.Usuario.Nombre} {_servicio.Usuario.Apellido1}";
                if (!string.IsNullOrEmpty(_servicio.Usuario.Apellido2))
                {
                    nombreCompleto += $" {_servicio.Usuario.Apellido2}";
                }
                NombreProveedorLabel.Text = nombreCompleto;

                // Ubicación del proveedor
                var ubicacion = "";
                if (_servicio.Usuario.Canton?.Provincia != null)
                {
                    ubicacion = $"{_servicio.Usuario.Canton.Nombre}, {_servicio.Usuario.Canton.Provincia.Nombre}";
                }
                else if (_servicio.Usuario.Provincia != null)
                {
                    ubicacion = _servicio.Usuario.Provincia.Nombre;
                }
                else
                {
                    ubicacion = "Costa Rica";
                }
                UbicacionProveedorLabel.Text = ubicacion;

                // Información adicional del proveedor (simulada)
                ServiciosCompletadosLabel.Text = "15 servicios";

                // Fecha de registro
                var fechaRegistro = _servicio.Usuario.CreatedAt.Year;
                MiembroDesdLabel.Text = $"Desde {fechaRegistro}";
            }
            else
            {
                NombreProveedorLabel.Text = "Proveedor no disponible";
                UbicacionProveedorLabel.Text = "Ubicación no disponible";
                ServiciosCompletadosLabel.Text = "N/A";
                MiembroDesdLabel.Text = "N/A";
            }

            System.Diagnostics.Debug.WriteLine("✅ Información del proveedor cargada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en información del proveedor: {ex.Message}");
        }
    }

    private void CargarSubcategorias()
    {
        try
        {
            SubcategoriasFlexLayout.Children.Clear();

            if (_servicio.SubCategorias?.Any() == true)
            {
                foreach (var subcategoria in _servicio.SubCategorias)
                {
                    var chip = CrearChipSubcategoria(subcategoria.Nombre);
                    SubcategoriasFlexLayout.Children.Add(chip);
                }

                SubcategoriasContainer.IsVisible = true;
                System.Diagnostics.Debug.WriteLine($"✅ {_servicio.SubCategorias.Count} subcategorías cargadas");
            }
            else
            {
                SubcategoriasContainer.IsVisible = false;
                System.Diagnostics.Debug.WriteLine("ℹ️ No hay subcategorías para mostrar");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando subcategorías: {ex.Message}");
            SubcategoriasContainer.IsVisible = false;
        }
    }

    private Frame CrearChipSubcategoria(string nombre)
    {
        var frame = new Frame
        {
            BackgroundColor = Color.FromArgb("#E3F2FD"),
            CornerRadius = 15,
            HasShadow = false,
            Padding = new Thickness(12, 6),
            Margin = new Thickness(0, 0, 8, 8)
        };

        var label = new Label
        {
            Text = nombre,
            FontSize = 12,
            TextColor = Color.FromArgb("#1976D2"),
            FontAttributes = FontAttributes.Bold
        };

        frame.Content = label;
        return frame;
    }

    private void CargarInformacionAdicional()
    {
        try
        {
            // Información del servicio
            ServicioIdLabel.Text = $"#{_servicio.ServicioId}";
            CategoriaInfoLabel.Text = _servicio.Categoria?.Nombre ?? "Sin categoría";

            // Fecha de publicación
            var diasTranscurridos = (DateTime.Now - _servicio.CreatedAt).Days;
            FechaPublicacionLabel.Text = diasTranscurridos switch
            {
                0 => "Hoy",
                1 => "Ayer",
                < 7 => $"Hace {diasTranscurridos} días",
                < 30 => $"Hace {diasTranscurridos / 7} semanas",
                _ => _servicio.CreatedAt.ToString("dd/MM/yyyy")
            };

            System.Diagnostics.Debug.WriteLine("✅ Información adicional cargada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en información adicional: {ex.Message}");
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

            IconoFrame.BackgroundColor = Color.FromArgb(color);

            System.Diagnostics.Debug.WriteLine($"✅ Icono configurado: {icono} con color {color}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error configurando icono: {ex.Message}");
            IconoFrame.BackgroundColor = Color.FromArgb("#A8D5BA");
        }
    }

    #endregion

    #region Eventos de UI

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnCompartirClicked(object sender, EventArgs e)
    {
        try
        {
            var mensaje = $"¡Mira este servicio en ServiFlex!\n\n" +
                         $"🔧 {_servicio.Titulo}\n" +
                         $"💰 ₡{_servicio.Precio:N0} por hora\n" +
                         $"📋 {(_servicio.Descripcion?.Length > 100 ? _servicio.Descripcion.Substring(0, 100) + "..." : _servicio.Descripcion ?? "Sin descripción")}\n\n" +
                         $"Descarga ServiFlex para más servicios";

            await Share.RequestAsync(new ShareTextRequest
            {
                Text = mensaje,
                Title = "Compartir Servicio"
            });

            System.Diagnostics.Debug.WriteLine("📤 Servicio compartido");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error compartiendo: {ex.Message}");
            await DisplayAlert("Error", "No se pudo compartir el servicio", "OK");
        }
    }

    private async void OnVerPerfilClicked(object sender, EventArgs e)
    {
        try
        {
            if (_servicio.Usuario != null)
            {
                var nombreCompleto = $"{_servicio.Usuario.Nombre} {_servicio.Usuario.Apellido1}";

                var info = $"Nombre: {nombreCompleto}\n\n";
                info += $"Email: {_servicio.Usuario.Correo}\n\n";
                info += $"Teléfono: {_servicio.Usuario.Telefono}\n\n";
                info += $"Ubicación: {UbicacionProveedorLabel.Text}\n\n";
                info += $"Miembro desde: {_servicio.Usuario.CreatedAt:yyyy}\n\n";
                info += $"Cuenta verificada: {(_servicio.Usuario.Verificacion > 0 ? "Sí" : "No")}";

                await DisplayAlert($"Perfil de {nombreCompleto}", info, "Cerrar");

                System.Diagnostics.Debug.WriteLine($"👤 Perfil visualizado: {nombreCompleto}");
            }
            else
            {
                await DisplayAlert("Error", "Información del proveedor no disponible", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error mostrando perfil: {ex.Message}");
            await DisplayAlert("Error", "No se pudo mostrar el perfil", "OK");
        }
    }

    #endregion

    #region Eventos de contacto

    private async void OnWhatsAppClicked(object sender, EventArgs e)
    {
        try
        {
            if (!SessionManager.EstaLogueado())
            {
                await DisplayAlert("Sesión requerida",
                    "Debes iniciar sesión para contactar al proveedor", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine("💬 WhatsApp clickeado");
            await ContactarWhatsApp();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en WhatsApp: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir WhatsApp", "OK");
        }
    }

    private async void OnEmailClicked(object sender, EventArgs e)
    {
        try
        {
            if (!SessionManager.EstaLogueado())
            {
                await DisplayAlert("Sesión requerida",
                    "Debes iniciar sesión para contactar al proveedor", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine("📧 Email clickeado");
            await ContactarEmail();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en email: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el email", "OK");
        }
    }

    private async void OnLlamadaClicked(object sender, EventArgs e)
    {
        try
        {
            if (!SessionManager.EstaLogueado())
            {
                await DisplayAlert("Sesión requerida",
                    "Debes iniciar sesión para contactar al proveedor", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine("📞 Llamada clickeada");
            await ContactarTelefono();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en llamada: {ex.Message}");
            await DisplayAlert("Error", "No se pudo iniciar la llamada", "OK");
        }
    }

    private async void OnChatClicked(object sender, EventArgs e)
    {
        try
        {
            if (!SessionManager.EstaLogueado())
            {
                await DisplayAlert("Sesión requerida",
                    "Debes iniciar sesión para usar el chat interno", "OK");
                return;
            }

            await DisplayAlert("Chat en ServiFlex",
                "La funcionalidad de chat interno estará disponible próximamente. " +
                "Por ahora puedes usar WhatsApp, email o llamada directa.", "OK");

            System.Diagnostics.Debug.WriteLine("💭 Chat interno solicitado");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en chat interno: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el chat", "OK");
        }
    }

    #endregion

    #region Métodos de contacto

    private async Task ContactarWhatsApp()
    {
        try
        {
            if (_servicio.Usuario?.Telefono != null)
            {
                var telefono = LimpiarTelefono(_servicio.Usuario.Telefono);
                var mensaje = Uri.EscapeDataString($"Hola! Me interesa tu servicio '{_servicio.Titulo}' en ServiFlex.");
                var url = $"https://wa.me/{telefono}?text={mensaje}";

                await Launcher.OpenAsync(url);
                System.Diagnostics.Debug.WriteLine($"📱 WhatsApp abierto: {telefono}");
            }
            else
            {
                await DisplayAlert("Error", "Número de teléfono no disponible", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error abriendo WhatsApp: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir WhatsApp", "OK");
        }
    }

    private async Task ContactarEmail()
    {
        try
        {
            if (_servicio.Usuario?.Correo != null)
            {
                var subject = Uri.EscapeDataString($"Consulta sobre servicio: {_servicio.Titulo}");
                var body = Uri.EscapeDataString($"Hola,\n\nMe interesa tu servicio '{_servicio.Titulo}' publicado en ServiFlex.\n\n¿Podrías darme más información?\n\nGracias.");
                var url = $"mailto:{_servicio.Usuario.Correo}?subject={subject}&body={body}";

                await Launcher.OpenAsync(url);
                System.Diagnostics.Debug.WriteLine($"📧 Email abierto: {_servicio.Usuario.Correo}");
            }
            else
            {
                await DisplayAlert("Error", "Email no disponible", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error abriendo email: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el cliente de email", "OK");
        }
    }

    private async Task ContactarTelefono()
    {
        try
        {
            if (_servicio.Usuario?.Telefono != null)
            {
                var telefono = LimpiarTelefono(_servicio.Usuario.Telefono);
                var url = $"tel:{telefono}";

                await Launcher.OpenAsync(url);
                System.Diagnostics.Debug.WriteLine($"☎️ Llamada iniciada: {telefono}");
            }
            else
            {
                await DisplayAlert("Error", "Número de teléfono no disponible", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error iniciando llamada: {ex.Message}");
            await DisplayAlert("Error", "No se pudo iniciar la llamada", "OK");
        }
    }

    private string LimpiarTelefono(string telefono)
    {
        if (string.IsNullOrEmpty(telefono))
            return "";

        // Remover espacios y caracteres especiales
        var numeroLimpio = telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");

        // Si es un número de Costa Rica (8 dígitos)
        if (numeroLimpio.Length == 8 && (numeroLimpio.StartsWith("6") || numeroLimpio.StartsWith("7") || numeroLimpio.StartsWith("8")))
        {
            return $"506{numeroLimpio}";
        }

        // Si ya tiene código de país
        if (numeroLimpio.StartsWith("506") && numeroLimpio.Length == 11)
        {
            return numeroLimpio;
        }

        return numeroLimpio;
    }

    #endregion

    #region Eventos de reseñas

    private async void OnEscribirResenaClicked(object sender, EventArgs e)
    {
        try
        {
            if (!SessionManager.EstaLogueado())
            {
                await DisplayAlert("Sesión requerida",
                    "Debes iniciar sesión para escribir una reseña", "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"📝 Navegando a crear reseña para: {_servicio.Titulo}");

            // Navegar a la página de crear reseña
            await Navigation.PushAsync(new CrearResenaPage(_servicio));

            System.Diagnostics.Debug.WriteLine("✅ Navegación exitosa a CrearResenaPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error navegando a crear reseña: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir la página de reseñas", "OK");
        }
    }

    private async void OnVerResenasClicked(object sender, EventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"👀 Ver reseñas del servicio: {_servicio.Titulo}");

            // Por ahora mostrar alert, en el futuro navegar a ResenasPage
           // await DisplayAlert("Ver Reseñas",
                //"Funcionalidad de ver todas las reseñas próximamente.\n\n" +
               // "Por ahora puedes escribir tu propia reseña.", "OK");

            // Futura implementación:
             await Navigation.PushAsync(new ResenasPage(_servicio));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error viendo reseñas: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir las reseñas", "OK");
        }
    }

    #endregion

    #region Carga de reseñas reales

    private async void CargarResenasReales()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🌟 Cargando reseñas reales para servicio: {_servicio.Titulo}");

            // ✅ LLAMAR A LA API REAL EN LUGAR DE DATOS SIMULADOS
            var response = await _apiService.ObtenerResenasPorServicioAsync(_servicio);

            if (response.Resultado && response.Resenas != null)
            {
                // Usar datos reales de la API
                _resenasDelServicio = response.Resenas;
                System.Diagnostics.Debug.WriteLine($"✅ {_resenasDelServicio.Count} reseñas reales cargadas");
            }
            else
            {
                // Si no hay reseñas o hay error, inicializar lista vacía
                _resenasDelServicio = new List<Resena>();
                var errorMsg = response.Error?.FirstOrDefault()?.Message ?? "No se encontraron reseñas";
                System.Diagnostics.Debug.WriteLine($"⚠️ {errorMsg}");
            }

            // Actualizar la UI con los datos reales
            ActualizarSeccionResenasConDatosReales();
            ActualizarResenasDestacadas();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando reseñas reales: {ex.Message}");

            // En caso de error, mostrar datos vacíos
            _resenasDelServicio = new List<Resena>();
            ActualizarSeccionResenasConDatosReales();
            ActualizarResenasDestacadas();
        }
    }

    private void ActualizarSeccionResenasConDatosReales()
    {
        try
        {
            if (!_resenasDelServicio.Any())
            {
                // No hay reseñas
                CalificacionLabel.Text = "N/A";
                ResenasLabel.Text = "(Sin reseñas)";
                System.Diagnostics.Debug.WriteLine("📊 Sin reseñas para mostrar");
                return;
            }

            // Calcular estadísticas reales
            var promedioCalificacion = _resenasDelServicio.Average(r => r.Calificacion);
            var totalResenas = _resenasDelServicio.Count;

            // Actualizar calificación promedio
            CalificacionLabel.Text = promedioCalificacion.ToString("F1");

            // Actualizar texto de reseñas
            ResenasLabel.Text = totalResenas == 1
                ? "(1 reseña)"
                : $"({totalResenas} reseñas)";

            System.Diagnostics.Debug.WriteLine($"✅ Estadísticas actualizadas - Promedio: {promedioCalificacion:F1}, Total: {totalResenas}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error actualizando sección de reseñas: {ex.Message}");

            // Mostrar valores por defecto en caso de error
            CalificacionLabel.Text = "N/A";
            ResenasLabel.Text = "(Error)";
        }
    }

    private void ActualizarResenasDestacadas()
    {
        try
        {
            // Limpiar contenedor de reseñas destacadas
            ResenasDestacadasStack.Children.Clear();

            if (!_resenasDelServicio.Any())
            {
                // Mostrar mensaje de sin reseñas
                SinResenasFrame.IsVisible = true;
                ResenasDestacadasContainer.IsVisible = true;
                System.Diagnostics.Debug.WriteLine("📝 Mostrando mensaje de sin reseñas");
                return;
            }

            // Ocultar mensaje de sin reseñas
            SinResenasFrame.IsVisible = false;

            // Obtener las 2 reseñas más útiles (con comentario y calificación alta)
            var resenasDestacadas = _resenasDelServicio
                .Where(r => !string.IsNullOrWhiteSpace(r.Comentario)) // Solo con comentario
                .OrderByDescending(r => r.Calificacion) // Ordenar por calificación
                .ThenByDescending(r => r.CreatedAt) // Luego por fecha
                .Take(2) // Tomar máximo 2
                .ToList();

            foreach (var resena in resenasDestacadas)
            {
                var resenaFrame = CrearFrameResenaDestacada(resena);
                ResenasDestacadasStack.Children.Add(resenaFrame);
            }

            System.Diagnostics.Debug.WriteLine($"✅ {resenasDestacadas.Count} reseñas destacadas mostradas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error actualizando reseñas destacadas: {ex.Message}");
        }
    }

    private Frame CrearFrameResenaDestacada(Resena resena)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E0E0E0"),
            CornerRadius = 10,
            HasShadow = true,
            Padding = 15,
            Margin = new Thickness(0, 5)
        };

        var stackLayout = new StackLayout { Spacing = 10 };

        // Header con usuario y calificación
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
        {
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = GridLength.Auto }
        }
        };

        // Nombre del usuario
        var nombreUsuario = $"{resena.Usuario?.Nombre} {resena.Usuario?.Apellido1}".Trim();
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            nombreUsuario = "Usuario";

        var usuarioLabel = new Label
        {
            Text = nombreUsuario,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black,
            VerticalOptions = LayoutOptions.Center
        };

        // Estrellas de calificación
        var estrellasLabel = new Label
        {
            Text = GenerarEstrellas(resena.Calificacion),
            FontSize = 14,
            TextColor = Color.FromArgb("#FFD700"),
            VerticalOptions = LayoutOptions.Center
        };

        headerGrid.Children.Add(usuarioLabel);
        Grid.SetColumn(usuarioLabel, 0);
        headerGrid.Children.Add(estrellasLabel);
        Grid.SetColumn(estrellasLabel, 1);

        stackLayout.Children.Add(headerGrid);

        // Comentario
        if (!string.IsNullOrWhiteSpace(resena.Comentario))
        {
            var comentarioLabel = new Label
            {
                Text = resena.Comentario,
                FontSize = 13,
                TextColor = Color.FromArgb("#666666"),
                LineBreakMode = LineBreakMode.WordWrap
            };
            stackLayout.Children.Add(comentarioLabel);
        }

        // Fecha
        var fechaLabel = new Label
        {
            Text = CalcularTiempoTranscurrido(resena.CreatedAt),
            FontSize = 11,
            TextColor = Color.FromArgb("#999999"),
            HorizontalOptions = LayoutOptions.End
        };
        stackLayout.Children.Add(fechaLabel);

        frame.Content = stackLayout;
        return frame;
    }

    private string GenerarEstrellas(int calificacion)
    {
        return string.Concat(Enumerable.Repeat("★", calificacion)) +
               string.Concat(Enumerable.Repeat("☆", 5 - calificacion));
    }

    private string CalcularTiempoTranscurrido(DateTime fecha)
    {
        var diferencia = DateTime.Now - fecha;

        if (diferencia.TotalDays >= 1)
            return $"Hace {(int)diferencia.TotalDays} día{(diferencia.TotalDays >= 2 ? "s" : "")}";
        else if (diferencia.TotalHours >= 1)
            return $"Hace {(int)diferencia.TotalHours} hora{(diferencia.TotalHours >= 2 ? "s" : "")}";
        else
            return "Hace menos de 1 hora";
    }

    #endregion

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Recargar reseñas cuando la página aparece (por si se agregó una nueva)
        CargarResenasReales();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cleanup
        _apiService?.Dispose();

        System.Diagnostics.Debug.WriteLine("👋 DetalleServicioPage desapareció");
    }

    #endregion
}