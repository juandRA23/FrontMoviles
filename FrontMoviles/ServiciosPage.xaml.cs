// FrontMoviles/ServiciosPage.xaml.cs - CÓDIGO COMPLETO
using FrontMoviles.Servicios;
using FrontMoviles.Modelos;
using System.Globalization;

namespace FrontMoviles;

public partial class ServiciosPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Servicio> _todosLosServicios = new List<Servicio>();
    private List<Servicio> _serviciosFiltrados = new List<Servicio>();
    private string _filtroActual = "Todos";

    public ServiciosPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        CargarServicios();
        ActualizarBotonesFiltro(TodosButton);
    }

    #region Carga de datos

    private async void CargarServicios()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Cargando servicios...");
            MostrarEstado("loading");

            var response = await _apiService.ObtenerServiciosAsync();

            if (response.Resultado && response.Servicios != null)
            {
                _todosLosServicios = response.Servicios;
                _serviciosFiltrados = new List<Servicio>(_todosLosServicios); // Update _serviciosFiltrados

                CrearListasPorCategoria();

                System.Diagnostics.Debug.WriteLine($"✅ Servicios cargados: {_todosLosServicios.Count}");

                if (_todosLosServicios.Any())
                {
                    CargarServiciosEnUI();
                    MostrarEstado("content");
                }
                else
                {
                    MostrarEstado("no-servicios");
                }
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando servicios: {errorMessage}");
                MostrarError($"Error al cargar servicios: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Excepción cargando servicios: {ex.Message}");
            MostrarError($"Error inesperado: {ex.Message}");
        }
    }

    private void CrearListasPorCategoria()
    {
        var tutorias = _todosLosServicios.Where(s => s.Categoria?.Nombre?.Equals("Tutorías", StringComparison.OrdinalIgnoreCase) == true).ToList();
        var mantenimientoReparaciones = _todosLosServicios.Where(s => s.Categoria?.Nombre?.Equals("Mantenimiento y Reparaciones", StringComparison.OrdinalIgnoreCase) == true).ToList();
        var cuidadoPersonal = _todosLosServicios.Where(s => s.Categoria?.Nombre?.Equals("Cuidado Personal", StringComparison.OrdinalIgnoreCase) == true).ToList();
        var tecnologiaElectronica = _todosLosServicios.Where(s => s.Categoria?.Nombre?.Equals("Tecnología y Electrónica", StringComparison.OrdinalIgnoreCase) == true).ToList();
        var serviciosDomesticos = _todosLosServicios.Where(s => s.Categoria?.Nombre?.Equals("Servicios Domésticos", StringComparison.OrdinalIgnoreCase) == true).ToList();

        System.Diagnostics.Debug.WriteLine($"📊 Distribución de servicios por categoría:");
        System.Diagnostics.Debug.WriteLine($"   • Tutorías: {tutorias.Count} servicios");
        System.Diagnostics.Debug.WriteLine($"   • Mantenimiento y Reparaciones: {mantenimientoReparaciones.Count} servicios");
        System.Diagnostics.Debug.WriteLine($"   • Cuidado Personal: {cuidadoPersonal.Count} servicios");
        System.Diagnostics.Debug.WriteLine($"   • Tecnología y Electrónica: {tecnologiaElectronica.Count} servicios");
        System.Diagnostics.Debug.WriteLine($"   • Servicios Domésticos: {serviciosDomesticos.Count} servicios");

        ServiciosTutorias = tutorias;
        ServiciosMantenimientoReparaciones = mantenimientoReparaciones;
        ServiciosCuidadoPersonal = cuidadoPersonal;
        ServiciosTecnologiaElectronica = tecnologiaElectronica;
        ServiciosDomesticos = serviciosDomesticos;
    }

    // Propiedades para almacenar las listas por categoría
    public List<Servicio> ServiciosTutorias { get; private set; } = new List<Servicio>();
    public List<Servicio> ServiciosMantenimientoReparaciones { get; private set; } = new List<Servicio>();
    public List<Servicio> ServiciosCuidadoPersonal { get; private set; } = new List<Servicio>();
    public List<Servicio> ServiciosTecnologiaElectronica { get; private set; } = new List<Servicio>();
    public List<Servicio> ServiciosDomesticos { get; private set; } = new List<Servicio>();

    private void CargarServiciosEnUI()
    {
        try
        {
            ServiciosContainer.Children.Clear();
            System.Diagnostics.Debug.WriteLine($"🔄 Actualizando UI con {_serviciosFiltrados.Count} servicios");

            foreach (var servicio in _serviciosFiltrados)
            {
                var servicioFrame = CrearServicioUI(servicio);
                ServiciosContainer.Children.Add(servicioFrame);
                System.Diagnostics.Debug.WriteLine($"✅ Agregado servicio: {servicio.Titulo}");
            }

            System.Diagnostics.Debug.WriteLine($"✅ UI actualizada con {_serviciosFiltrados.Count} servicios");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error creando UI: {ex.Message}");
            DisplayAlert("Error", $"Error al mostrar servicios: {ex.Message}", "OK");
        }
    }

    private Frame CrearServicioUI(Servicio servicio)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E0E0E0"),
            CornerRadius = 12,
            HasShadow = true,
            Padding = 15,
            Margin = new Thickness(0, 5)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(60) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(80) }
            }
        };

        // Icono del servicio basado en categoría
        var iconFrame = new Frame
        {
            BackgroundColor = ObtenerColorCategoria(servicio.Categoria?.Nombre),
            CornerRadius = 10,
            WidthRequest = 50,
            HeightRequest = 50,
            HasShadow = false,
            Padding = 0
        };

        var iconLabel = new Label
        {
            Text = ObtenerIconoCategoria(servicio.Categoria?.Nombre),
            FontSize = 20,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        iconFrame.Content = iconLabel;
        grid.SetColumn(iconFrame, 0);
        grid.Children.Add(iconFrame);

        // Información del servicio
        var infoStack = new StackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(15, 0, 0, 0)
        };

        // Categoría
        if (servicio.Categoria != null)
        {
            infoStack.Children.Add(new Label
            {
                Text = servicio.Categoria.Nombre.ToUpper(),
                FontSize = 12,
                TextColor = Color.FromArgb("#4A90A4"),
                FontAttributes = FontAttributes.Bold
            });
        }

        // Título
        infoStack.Children.Add(new Label
        {
            Text = servicio.Titulo,
            FontSize = 16,
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation
        });

        // Descripción (truncada)
        var descripcionCorta = servicio.Descripcion?.Length > 60
            ? $"{servicio.Descripcion.Substring(0, 60)}..."
            : servicio.Descripcion;

        infoStack.Children.Add(new Label
        {
            Text = descripcionCorta,
            FontSize = 13,
            TextColor = Colors.Gray,
            LineBreakMode = LineBreakMode.TailTruncation
        });

        // Usuario
        if (servicio.Usuario != null)
        {
            var nombreCompleto = $"{servicio.Usuario.Nombre} {servicio.Usuario.Apellido1}";
            infoStack.Children.Add(new Label
            {
                Text = $"Por: {nombreCompleto}",
                FontSize = 12,
                TextColor = Color.FromArgb("#666666"),
                FontAttributes = FontAttributes.Italic
            });
        }

        grid.SetColumn(infoStack, 1);
        grid.Children.Add(infoStack);

        // Precio y disponibilidad
        var precioStack = new StackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End,
            Spacing = 5
        };

        precioStack.Children.Add(new Label
        {
            Text = $"₡{servicio.Precio:N0}/hr",
            FontSize = 16,
            TextColor = Color.FromArgb("#4A7C59"),
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.End
        });

        // Indicador de disponibilidad
        var disponibilidadFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#E8F5E8"),
            CornerRadius = 8,
            HasShadow = false,
            Padding = new Thickness(8, 4)
        };

        disponibilidadFrame.Content = new Label
        {
            Text = "Disponible",
            FontSize = 10,
            TextColor = Color.FromArgb("#4A7C59"),
            FontAttributes = FontAttributes.Bold
        };

        precioStack.Children.Add(disponibilidadFrame);

        grid.SetColumn(precioStack, 2);
        grid.Children.Add(precioStack);

        frame.Content = grid;

        // ✅ AGREGAR GESTO DE TAP - NAVEGACIÓN A DETALLE
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnServicioClicked(servicio);
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    private Color ObtenerColorCategoria(string categoria)
    {
        return categoria?.ToLower() switch
        {
            "educación" or "educacion" => Color.FromArgb("#6C7CE7"),
            "tecnología" or "tecnologia" => Color.FromArgb("#34C759"),
            "hogar" => Color.FromArgb("#FF9500"),
            "diseño" or "diseno" => Color.FromArgb("#FF2D92"),
            "salud" => Color.FromArgb("#00C7BE"),
            _ => Color.FromArgb("#A8D5BA")
        };
    }

    private string ObtenerIconoCategoria(string categoria)
    {
        return categoria?.ToLower() switch
        {
            "educación" or "educacion" => "📚",
            "tecnología" or "tecnologia" => "💻",
            "hogar" => "🏠",
            "diseño" or "diseno" => "🎨",
            "salud" => "⚕️",
            _ => "🔧"
        };
    }

    private void MostrarEstado(string estado)
    {
        LoadingGrid.IsVisible = estado == "loading";
        ContentRefreshView.IsVisible = estado == "content";
        ErrorGrid.IsVisible = estado == "error";
        NoServiciosContainer.IsVisible = estado == "no-servicios";

        if (estado == "no-servicios")
        {
            ContentRefreshView.IsVisible = true;
            ServiciosContainer.IsVisible = false;
        }
        else if (estado == "content")
        {
            ServiciosContainer.IsVisible = true;
            NoServiciosContainer.IsVisible = false;
        }

        System.Diagnostics.Debug.WriteLine($"🔄 Estado UI: {estado}");
    }

    private void MostrarError(string mensaje)
    {
        ErrorMessageLabel.Text = mensaje;
        MostrarEstado("error");
    }

    #endregion

    #region Eventos de UI

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnFiltrosClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Filtros", "Función de filtros avanzados próximamente", "OK");
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await Task.Delay(1000); // Simular carga
            CargarServicios();
        }
        finally
        {
            ContentRefreshView.IsRefreshing = false;
        }
    }

    private async void OnReintentarClicked(object sender, EventArgs e)
    {
        CargarServicios();
    }

    private async void OnPublicarServicioClicked(object sender, EventArgs e)
    {
        if (!SessionManager.EstaLogueado())
        {
            await DisplayAlert("Sesión requerida", "Debes iniciar sesión para publicar servicios", "OK");
            return;
        }

        await Navigation.PushAsync(new PublicarServicioPage());
    }

    #endregion

    #region Filtros


    private void AplicarFiltro(string filtro)
    {
        try
        {
            if (filtro == "Todos")
            {
                _serviciosFiltrados = new List<Servicio>(_todosLosServicios); // Update _serviciosFiltrados
            }
            else
            {
                _serviciosFiltrados = _todosLosServicios
                    .Where(s => s.Categoria?.Nombre?.Equals(filtro, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }

            CargarServiciosEnUI();

            if (!_serviciosFiltrados.Any())
            {
                MostrarEstado("no-servicios");
            }
            else
            {
                MostrarEstado("content");
            }

            System.Diagnostics.Debug.WriteLine($"🔍 Filtro aplicado: {filtro}, Resultados: {_serviciosFiltrados.Count}");
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
        TodosButton,
        TutoriasButton,
        MantenimientoButton,
        CuidadoPersonalButton,
        TecnologiaButton,
        DomesticosButton
    };

        // Resetear todos los botones al estilo no seleccionado
        foreach (var boton in botonesFiltro)
        {
            boton.BackgroundColor = Color.FromHex("#FFFFFF"); // Fondo blanco/transparente
            boton.TextColor = Color.FromHex("#4A7C59");       // Texto verde
            boton.BorderColor = Color.FromHex("#4A7C59");     // Borde verde
            boton.BorderWidth = 1;
        }

        // Aplicar estilo seleccionado al botón activo
        if (botonSeleccionado != null)
        {
            botonSeleccionado.BackgroundColor = Color.FromHex("#4A7C59"); // Fondo verde
            botonSeleccionado.TextColor = Color.FromHex("#FFFFFF");       // Texto blanco
            botonSeleccionado.BorderColor = Color.FromHex("#4A7C59");     // Borde verde
            botonSeleccionado.BorderWidth = 0; // Sin borde para el seleccionado
        }
    }

    private void OnFiltroClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string filtro)
        {
            _filtroActual = filtro;
            AplicarFiltro(filtro);
            ActualizarBotonesFiltro(button);
        }
    }
    #endregion

    #region Eventos de servicios - ✅ NAVEGACIÓN A DETALLE CORREGIDA

    private async void OnServicioClicked(Servicio servicio)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"👆 NAVEGANDO A DETALLE: {servicio.Titulo} (ID: {servicio.ServicioId})");

            // ✅ NAVEGAR A LA PÁGINA DE DETALLE DEL SERVICIO
            await Navigation.PushAsync(new DetalleServicioPage(servicio));

            System.Diagnostics.Debug.WriteLine("✅ Navegación exitosa a DetalleServicioPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ ERROR navegando a detalle: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Error al abrir detalle del servicio: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Cleanup

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _apiService?.Dispose();
    }

    #endregion
}