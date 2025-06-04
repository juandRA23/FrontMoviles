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
    }

    #region Carga de datos

    private async void CargarServicios()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Cargando servicios...");

            // Mostrar indicador de carga
            MostrarEstado("loading");

            // Llamar a la API
            var response = await _apiService.ObtenerServiciosAsync();

            if (response.Resultado && response.Servicios != null)
            {
                _todosLosServicios = response.Servicios;
                _serviciosFiltrados = new List<Servicio>(_todosLosServicios);

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

    private void CargarServiciosEnUI()
    {
        try
        {
            ServiciosContainer.Children.Clear();

            foreach (var servicio in _serviciosFiltrados)
            {
                var servicioFrame = CrearServicioUI(servicio);
                ServiciosContainer.Children.Add(servicioFrame);
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

        // Agregar gesto de tap
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
            if (filtro == "Todos")
            {
                _serviciosFiltrados = new List<Servicio>(_todosLosServicios);
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
        // Buscar todos los botones de filtro y resetear su estilo
        var filtrosFrame = this.FindByName<Frame>("filtrosFrame");
        // Implementar lógica para cambiar estilos de botones
        // Por simplicidad, se mantiene el estilo actual
    }

    #endregion

    #region Eventos de servicios

    private async void OnServicioClicked(Servicio servicio)
    {
        try
        {
            // Crear información detallada del servicio
            var info = $"Título: {servicio.Titulo}\n\n";
            info += $"Descripción: {servicio.Descripcion}\n\n";
            info += $"Precio: ₡{servicio.Precio:N0} por hora\n\n";
            info += $"Disponibilidad: {servicio.Disponibilidad}\n\n";

            if (servicio.Usuario != null)
            {
                info += $"Proveedor: {servicio.Usuario.Nombre} {servicio.Usuario.Apellido1}\n\n";
            }

            if (servicio.SubCategorias?.Any() == true)
            {
                info += $"Especialidades: {string.Join(", ", servicio.SubCategorias.Select(sc => sc.Nombre))}";
            }

            await DisplayAlert($"Servicio: {servicio.Titulo}", info, "Cerrar");

            // Aquí podrías navegar a una página de detalle del servicio
            System.Diagnostics.Debug.WriteLine($"👆 Servicio seleccionado: {servicio.Titulo} (ID: {servicio.ServicioId})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error mostrando detalle: {ex.Message}");
            await DisplayAlert("Error", "Error al mostrar detalles del servicio", "OK");
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