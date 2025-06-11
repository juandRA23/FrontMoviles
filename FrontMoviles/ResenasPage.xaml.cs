using FrontMoviles.Servicios;
using FrontMoviles.Modelos;
using System.Globalization;

namespace FrontMoviles;

public partial class ResenasPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly Servicio _servicio;
    private List<Resena> _todasLasResenas = new List<Resena>();
    private List<Resena> _resenasFiltradas = new List<Resena>();
    private string _filtroActual = "Todas";

    public ResenasPage(Servicio servicio)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _servicio = servicio ?? throw new ArgumentNullException(nameof(servicio));

        CargarInformacionServicio();
        CargarResenas();
    }

    #region Configuración inicial

    private void CargarInformacionServicio()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"📄 Cargando información del servicio: {_servicio.Titulo}");

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

            System.Diagnostics.Debug.WriteLine("✅ Información del servicio cargada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando información del servicio: {ex.Message}");
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

    private async void CargarResenas()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Cargando reseñas...");
            MostrarEstado("loading");

            var response = await _apiService.ObtenerResenasPorServicioAsync(_servicio);

            if (response.Resultado && response.Resenas != null)
            {
                _todasLasResenas = response.Resenas;
                _resenasFiltradas = new List<Resena>(_todasLasResenas);

                System.Diagnostics.Debug.WriteLine($"✅ Reseñas cargadas: {_todasLasResenas.Count}");

                ActualizarEstadisticas();
                ActualizarBotonesFiltro(TodasButton);

                if (_todasLasResenas.Any())
                {
                    CargarResenasEnUI();
                    MostrarEstado("content");
                }
                else
                {
                    MostrarEstado("no-resenas");
                }
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando reseñas: {errorMessage}");
                MostrarError($"Error al cargar reseñas: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Excepción cargando reseñas: {ex.Message}");
            MostrarError($"Error inesperado: {ex.Message}");
        }
    }

    private void ActualizarEstadisticas()
    {
        try
        {
            if (!_todasLasResenas.Any())
            {
                CalificacionPromedioLabel.Text = "N/A";
                TotalResenasLabel.Text = "Sin reseñas";
                return;
            }

            // Calcular promedio
            var promedio = _todasLasResenas.Average(r => r.Calificacion);
            CalificacionPromedioLabel.Text = promedio.ToString("F1");

            // Total de reseñas
            var total = _todasLasResenas.Count;
            TotalResenasLabel.Text = total == 1 ? "1 reseña" : $"{total} reseñas";

            System.Diagnostics.Debug.WriteLine($"📊 Estadísticas: Promedio {promedio:F1}, Total {total}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error actualizando estadísticas: {ex.Message}");
        }
    }

    private void CargarResenasEnUI()
    {
        try
        {
            ResenasContainer.Children.Clear();
            System.Diagnostics.Debug.WriteLine($"🔄 Actualizando UI con {_resenasFiltradas.Count} reseñas");

            // Ordenar por fecha más reciente
            var resenasOrdenadas = _resenasFiltradas.OrderByDescending(r => r.CreatedAt).ToList();

            foreach (var resena in resenasOrdenadas)
            {
                var resenaFrame = CrearResenaUI(resena);
                ResenasContainer.Children.Add(resenaFrame);
            }

            System.Diagnostics.Debug.WriteLine($"✅ UI actualizada con {_resenasFiltradas.Count} reseñas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error creando UI: {ex.Message}");
            DisplayAlert("Error", $"Error al mostrar reseñas: {ex.Message}", "OK");
        }
    }

    private Frame CrearResenaUI(Resena resena)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E0E0E0"),
            CornerRadius = 12,
            HasShadow = true,
            Padding = 20,
            Margin = new Thickness(0, 5)
        };

        var stackLayout = new StackLayout
        {
            Spacing = 15
        };

        // Header con usuario y fecha
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(50) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(60) }
            }
        };

        // Avatar del usuario
        var avatarFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#E0E0E0"),
            CornerRadius = 25,
            WidthRequest = 50,
            HeightRequest = 50,
            HasShadow = false,
            Padding = 0
        };

        var avatarLabel = new Label
        {
            Text = "👤",
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        avatarFrame.Content = avatarLabel;
        headerGrid.SetColumn(avatarFrame, 0);
        headerGrid.Children.Add(avatarFrame);

        // Información del usuario
        var userInfoStack = new StackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(15, 0, 0, 0)
        };

        // Nombre del usuario
        if (resena.Usuario != null)
        {
            var nombreCompleto = $"{resena.Usuario.Nombre} {resena.Usuario.Apellido1}";
            userInfoStack.Children.Add(new Label
            {
                Text = nombreCompleto,
                FontSize = 16,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold
            });
        }

        // Estrellas y fecha
        var ratingDateStack = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 10
        };

        var estrellas = GenerarEstrellas(resena.Calificacion);
        ratingDateStack.Children.Add(new Label
        {
            Text = estrellas,
            FontSize = 14,
            TextColor = Color.FromArgb("#FFD700"),
            VerticalOptions = LayoutOptions.Center
        });

        var diasTranscurridos = (DateTime.Now - resena.CreatedAt).Days;
        var fechaTexto = diasTranscurridos switch
        {
            0 => "Hoy",
            1 => "Ayer",
            < 7 => $"Hace {diasTranscurridos} días",
            < 30 => $"Hace {diasTranscurridos / 7} semanas",
            _ => resena.CreatedAt.ToString("dd/MM/yyyy")
        };

        ratingDateStack.Children.Add(new Label
        {
            Text = $"• {fechaTexto}",
            FontSize = 12,
            TextColor = Colors.Gray,
            VerticalOptions = LayoutOptions.Center
        });

        userInfoStack.Children.Add(ratingDateStack);

        headerGrid.SetColumn(userInfoStack, 1);
        headerGrid.Children.Add(userInfoStack);

        // Calificación numérica
        var calificacionLabel = new Label
        {
            Text = resena.Calificacion.ToString(),
            FontSize = 20,
            TextColor = Color.FromArgb("#4A7C59"),
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        headerGrid.SetColumn(calificacionLabel, 2);
        headerGrid.Children.Add(calificacionLabel);

        stackLayout.Children.Add(headerGrid);

        // Comentario (si existe)
        if (!string.IsNullOrWhiteSpace(resena.Comentario))
        {
            stackLayout.Children.Add(new Label
            {
                Text = resena.Comentario,
                FontSize = 14,
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        frame.Content = stackLayout;
        return frame;
    }

    private string GenerarEstrellas(int calificacion)
    {
        var estrellas = "";
        for (int i = 1; i <= 5; i++)
        {
            estrellas += i <= calificacion ? "★" : "☆";
        }
        return estrellas;
    }

    #endregion

    #region Eventos de UI

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

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

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await Task.Delay(1000); // Simular carga
            CargarResenas();
        }
        finally
        {
            ContentRefreshView.IsRefreshing = false;
        }
    }

    private async void OnReintentarClicked(object sender, EventArgs e)
    {
        CargarResenas();
    }

    #endregion

    #region Filtros

    private void OnFiltroClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string filtro)
        {
            _filtroActual = filtro;
            AplicarFiltro(filtro);
            ActualizarBotonesFiltro(button);
        }
    }

    private void AplicarFiltro(string filtro)
    {
        try
        {
            if (filtro == "Todas")
            {
                _resenasFiltradas = new List<Resena>(_todasLasResenas);
            }
            else if (int.TryParse(filtro, out int calificacion))
            {
                _resenasFiltradas = _todasLasResenas
                    .Where(r => r.Calificacion == calificacion)
                    .ToList();
            }

            CargarResenasEnUI();

            if (!_resenasFiltradas.Any())
            {
                MostrarEstado("no-resenas");
            }
            else
            {
                MostrarEstado("content");
            }

            System.Diagnostics.Debug.WriteLine($"🔍 Filtro aplicado: {filtro}, Resultados: {_resenasFiltradas.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error aplicando filtro: {ex.Message}");
            DisplayAlert("Error", "Error al aplicar filtro", "OK");
        }
    }

    private void ActualizarBotonesFiltro(Button botonSeleccionado)
    {
        // Lista de todos los botones de filtro
        var botonesFiltro = new List<Button>
        {
            TodasButton,
            Cinco5Button,
            Cuatro4Button,
            Tres3Button,
            Dos2Button,
            Uno1Button
        };

        // Resetear todos los botones al estilo no seleccionado
        foreach (var boton in botonesFiltro)
        {
            boton.BackgroundColor = Color.FromHex("#FFFFFF");
            boton.TextColor = Color.FromHex("#4A7C59");
            boton.BorderColor = Color.FromHex("#4A7C59");
            boton.BorderWidth = 1;
        }

        // Aplicar estilo seleccionado al botón activo
        if (botonSeleccionado != null)
        {
            botonSeleccionado.BackgroundColor = Color.FromHex("#4A7C59");
            botonSeleccionado.TextColor = Color.FromHex("#FFFFFF");
            botonSeleccionado.BorderColor = Color.FromHex("#4A7C59");
            botonSeleccionado.BorderWidth = 0;
        }
    }

    #endregion

    #region Estados de UI

    private void MostrarEstado(string estado)
    {
        LoadingGrid.IsVisible = estado == "loading";
        ContentRefreshView.IsVisible = estado == "content";
        ErrorGrid.IsVisible = estado == "error";
        NoResenasContainer.IsVisible = estado == "no-resenas";

        if (estado == "no-resenas")
        {
            ContentRefreshView.IsVisible = true;
            ResenasContainer.IsVisible = false;
        }
        else if (estado == "content")
        {
            ResenasContainer.IsVisible = true;
            NoResenasContainer.IsVisible = false;
        }

        System.Diagnostics.Debug.WriteLine($"🔄 Estado UI: {estado}");
    }

    private void MostrarError(string mensaje)
    {
        ErrorMessageLabel.Text = mensaje;
        MostrarEstado("error");
        System.Diagnostics.Debug.WriteLine($"❌ Error mostrado: {mensaje}");
    }

    #endregion

    #region Cleanup

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _apiService?.Dispose();
        System.Diagnostics.Debug.WriteLine("🚪 ResenasPage cerrada");
    }

    #endregion
}