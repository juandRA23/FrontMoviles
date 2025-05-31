using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class PublicarServicioPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Categoria> _categorias = new List<Categoria>();
    private List<SubCategoria> _subCategorias = new List<SubCategoria>();
    private List<SubCategoriaSeleccionada> _subCategoriasSeleccionadas = new List<SubCategoriaSeleccionada>();

    public PublicarServicioPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        CargarDatos();
    }

    #region Carga de datos iniciales

    private async void CargarDatos()
    {
        try
        {
            // Verificar sesión
            if (!SessionManager.EstaLogueado())
            {
                MostrarError("No hay sesión activa. Por favor, inicia sesión.");
                await Task.Delay(2000);
                await RegresarALogin();
                return;
            }

            MostrarEstado("loading");

            // Cargar categorías y subcategorías en paralelo
            var categoriesTask = _apiService.ObtenerCategoriasAsync();
            var subCategoriesTask = _apiService.ObtenerSubCategoriasAsync();

            await Task.WhenAll(categoriesTask, subCategoriesTask);

            var categoriesResponse = await categoriesTask;
            var subCategoriesResponse = await subCategoriesTask;

            // Verificar respuestas
            if (!categoriesResponse.Resultado)
            {
                var errorMsg = categoriesResponse.Error?.FirstOrDefault()?.Message ?? "Error al cargar categorías";
                MostrarError(errorMsg);
                return;
            }

            if (!subCategoriesResponse.Resultado)
            {
                var errorMsg = subCategoriesResponse.Error?.FirstOrDefault()?.Message ?? "Error al cargar subcategorías";
                MostrarError(errorMsg);
                return;
            }

            // Guardar datos
            _categorias = categoriesResponse.Categorias ?? new List<Categoria>();
            _subCategorias = subCategoriesResponse.SubCategorias ?? new List<SubCategoria>();

            // Configurar UI
            ConfigurarCategorias();
            MostrarEstado("content");
        }
        catch (Exception ex)
        {
            MostrarError($"Error inesperado: {ex.Message}");
        }
    }

    private void ConfigurarCategorias()
    {
        try
        {
            // Limpiar picker
            CategoriaPicker.Items.Clear();

            // Agregar categorías al picker
            foreach (var categoria in _categorias)
            {
                CategoriaPicker.Items.Add(categoria.Nombre);
            }

            // Configurar subcategorías como no seleccionadas
            _subCategoriasSeleccionadas = _subCategorias.Select(sc => new SubCategoriaSeleccionada
            {
                SubCategoriaId = sc.SubCategoriaId,
                Nombre = sc.Nombre,
                IsSelected = false,
                CategoriaId = sc.Categoria?.CategoriaId ?? 0
            }).ToList();
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Error al configurar categorías: {ex.Message}", "OK");
        }
    }

    private async Task RegresarALogin()
    {
        try
        {
            SessionManager.CerrarSesion();
            Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al regresar al login: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Manejo de categorías y subcategorías

    private void OnCategoriaSelectionChanged(object sender, EventArgs e)
    {
        try
        {
            var picker = sender as Picker;
            if (picker?.SelectedIndex >= 0 && picker.SelectedIndex < _categorias.Count)
            {
                var categoriaSeleccionada = _categorias[picker.SelectedIndex];
                ActualizarSubCategorias(categoriaSeleccionada.CategoriaId);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Error al seleccionar categoría: {ex.Message}", "OK");
        }
    }

    private void ActualizarSubCategorias(int categoriaId)
    {
        try
        {
            // Limpiar contenedor
            SubCategoriasContainer.Children.Clear();

            // Filtrar subcategorías de la categoría seleccionada
            var subCategoriasCategoria = _subCategoriasSeleccionadas
                .Where(sc => sc.CategoriaId == categoriaId)
                .ToList();

            if (!subCategoriasCategoria.Any())
            {
                SubCategoriasContainer.Children.Add(new Label
                {
                    Text = "No hay subcategorías disponibles para esta categoría",
                    TextColor = Colors.Gray,
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                });
                return;
            }

            // Crear checkboxes para cada subcategoría
            foreach (var subCategoria in subCategoriasCategoria)
            {
                var stackLayout = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10
                };

                var checkBox = new CheckBox
                {
                    IsChecked = subCategoria.IsSelected,
                    Color = Color.FromArgb("#4A7C59")
                };

                var label = new Label
                {
                    Text = subCategoria.Nombre,
                    FontSize = 14,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black
                };

                // Evento para actualizar selección
                checkBox.CheckedChanged += (s, e) =>
                {
                    subCategoria.IsSelected = e.Value;
                };

                stackLayout.Children.Add(checkBox);
                stackLayout.Children.Add(label);
                SubCategoriasContainer.Children.Add(stackLayout);
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Error al actualizar subcategorías: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Validaciones

    private bool ValidarFormulario()
    {
        var errores = new List<string>();

        // Validar título
        if (string.IsNullOrWhiteSpace(TituloEntry.Text))
            errores.Add("El título es requerido");

        // Validar descripción
        if (string.IsNullOrWhiteSpace(DescripcionEditor.Text))
            errores.Add("La descripción es requerida");

        // Validar precio
        if (string.IsNullOrWhiteSpace(PrecioEntry.Text) || !decimal.TryParse(PrecioEntry.Text, out decimal precio) || precio <= 0)
            errores.Add("El precio debe ser un número mayor a 0");

        // Validar disponibilidad
        if (string.IsNullOrWhiteSpace(DisponibilidadEntry.Text))
            errores.Add("La disponibilidad es requerida");

        // Validar categoría
        if (CategoriaPicker.SelectedIndex < 0)
            errores.Add("Debe seleccionar una categoría");

        if (errores.Any())
        {
            DisplayAlert("Errores de validación", string.Join("\n", errores), "OK");
            return false;
        }

        return true;
    }

    #endregion

    #region Eventos de botones

    private async void OnPublicarClicked(object sender, EventArgs e)
    {
        try
        {
            if (!ValidarFormulario())
                return;

            // Deshabilitar botón
            PublicarButton.IsEnabled = false;
            PublicarButton.Text = "Publicando...";

            // Crear el servicio
            var resultado = await CrearServicio();

            if (resultado)
            {
                await DisplayAlert("Éxito", "Servicio publicado exitosamente", "OK");
                await Navigation.PopAsync(); // Regresar a la página anterior
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al publicar servicio: {ex.Message}", "OK");
        }
        finally
        {
            // Rehabilitar botón
            PublicarButton.IsEnabled = true;
            PublicarButton.Text = "Publicar Servicio";
        }
    }

    private async void OnGuardarBorradorClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Borrador", "Función de guardar borrador en desarrollo", "OK");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        bool salir = await DisplayAlert(
            "Confirmar",
            "¿Estás seguro que deseas salir? Los cambios no guardados se perderán.",
            "Sí",
            "Cancelar");

        if (salir)
        {
            await Navigation.PopAsync();
        }
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        await DisplayAlert(
            "Ayuda",
            "Consejos para publicar un buen servicio:\n\n" +
            "• Usa un título claro y descriptivo\n" +
            "• Describe detalladamente qué incluye tu servicio\n" +
            "• Especifica tu experiencia y metodología\n" +
            "• Define claramente tu disponibilidad\n" +
            "• Selecciona las categorías más relevantes",
            "OK");
    }

    private async void OnReintentarClicked(object sender, EventArgs e)
    {
        CargarDatos();
    }

    #endregion

    #region Creación de servicio

    private async Task<bool> CrearServicio()
    {
        try
        {
            // Obtener categoría seleccionada
            var categoriaSeleccionada = _categorias[CategoriaPicker.SelectedIndex];

            // Obtener subcategorías seleccionadas
            var subCategoriasSeleccionadas = _subCategoriasSeleccionadas
                .Where(sc => sc.IsSelected)
                .Select(sc => _subCategorias.First(sub => sub.SubCategoriaId == sc.SubCategoriaId))
                .ToList();

            // Crear el objeto servicio
            var servicio = new Servicio
            {
                ServicioId = 0, // Nuevo servicio
                Titulo = TituloEntry.Text?.Trim(),
                Descripcion = DescripcionEditor.Text?.Trim(),
                Precio = decimal.Parse(PrecioEntry.Text),
                Disponibilidad = DisponibilidadEntry.Text?.Trim(),
                Categoria = categoriaSeleccionada,
                SubCategorias = subCategoriasSeleccionadas,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Crear el request
            var request = new ReqInsertarServicio
            {
                SesionId = SessionManager.ObtenerSessionId(),
                Servicio = servicio
            };

            // Llamar a la API
            var response = await _apiService.CrearServicioAsync(request);

            if (response.Resultado)
            {
                return true;
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";

                // Si el error indica sesión inválida, regresar al login
                if (errorMessage.Contains("sesión") || errorMessage.Contains("autorizado") ||
                    errorMessage.Contains("token") || errorMessage.Contains("inválida"))
                {
                    await DisplayAlert("Sesión Expirada", errorMessage, "OK");
                    await RegresarALogin();
                    return false;
                }

                await DisplayAlert("Error", $"Error al crear servicio: {errorMessage}", "OK");
                return false;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
            return false;
        }
    }

    #endregion

    #region Estados de UI

    private void MostrarEstado(string estado)
    {
        LoadingGrid.IsVisible = estado == "loading";
        ContentScrollView.IsVisible = estado == "content";
        ErrorGrid.IsVisible = estado == "error";
    }

    private void MostrarError(string mensaje)
    {
        ErrorMessageLabel.Text = mensaje;
        MostrarEstado("error");
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