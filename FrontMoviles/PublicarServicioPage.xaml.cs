using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class PublicarServicioPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<Categoria> _categorias = new List<Categoria>();
    private List<SubCategoria> _subCategorias = new List<SubCategoria>();
    private List<SubCategoria> _subCategoriasFiltradas = new List<SubCategoria>();
    private List<SubCategoriaSeleccionada> _subCategoriasSeleccionadas = new List<SubCategoriaSeleccionada>();

    public PublicarServicioPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        CargarDatosIniciales();
    }

    #region Configuración Inicial

    private async void CargarDatosIniciales()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 Iniciando carga de datos para PublicarServicio");

            // Verificar sesión antes de cargar
            if (!SessionManager.EstaLogueado())
            {
                System.Diagnostics.Debug.WriteLine("❌ No hay sesión activa en PublicarServicio");
                await DisplayAlert("Sesión requerida", "Debes iniciar sesión para publicar servicios", "OK");
                await Navigation.PopAsync();
                return;
            }

            var userEmail = SessionManager.ObtenerEmailUsuario();
            System.Diagnostics.Debug.WriteLine($"✅ Usuario logueado: {userEmail}");

            // Mostrar loading
            MostrarEstado("loading");

            // Cargar categorías y subcategorías en paralelo
            var taskCategorias = _apiService.ObtenerCategoriasAsync();
            var taskSubCategorias = _apiService.ObtenerSubCategoriasAsync();

            await Task.WhenAll(taskCategorias, taskSubCategorias);

            var responseCategorias = await taskCategorias;
            var responseSubCategorias = await taskSubCategorias;

            // Procesar categorías
            if (responseCategorias.Resultado && responseCategorias.Categorias?.Any() == true)
            {
                _categorias = responseCategorias.Categorias;
                System.Diagnostics.Debug.WriteLine($"✅ Categorías cargadas: {_categorias.Count}");
                CargarCategoriasPicker();
            }
            else
            {
                var errorMsg = responseCategorias.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando categorías: {errorMsg}");
                MostrarError($"Error al cargar categorías: {errorMsg}");
                return;
            }

            // Procesar subcategorías
            if (responseSubCategorias.Resultado && responseSubCategorias.SubCategorias?.Any() == true)
            {
                _subCategorias = responseSubCategorias.SubCategorias;
                System.Diagnostics.Debug.WriteLine($"✅ SubCategorías cargadas: {_subCategorias.Count}");
            }
            else
            {
                var errorMsg = responseSubCategorias.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                System.Diagnostics.Debug.WriteLine($"⚠️ Advertencia subcategorías: {errorMsg}");
                await DisplayAlert("Advertencia", $"No se pudieron cargar subcategorías: {errorMsg}", "OK");
                // Continuar sin subcategorías
            }

            // Mostrar contenido
            MostrarEstado("content");
            System.Diagnostics.Debug.WriteLine("✅ PublicarServicio cargado correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Error en CargarDatosIniciales: {ex.Message}");
            MostrarError($"Error inesperado: {ex.Message}");
        }
    }

    private void CargarCategoriasPicker()
    {
        try
        {
            CategoriaPicker.ItemsSource = null;
            CategoriaPicker.ItemsSource = _categorias.Select(c => c.Nombre).ToList();
            CategoriaPicker.Title = "Seleccionar categoría";
            System.Diagnostics.Debug.WriteLine("✅ Picker de categorías configurado");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error configurando picker: {ex.Message}");
            DisplayAlert("Error", $"Error al cargar categorías en picker: {ex.Message}", "OK");
        }
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

    #region Eventos de UI

    private async void OnBackClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("⬅️ Regresando de PublicarServicio");
        await Navigation.PopAsync();
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("❓ Mostrando ayuda");
        await DisplayAlert("Ayuda",
            "• Completa todos los campos obligatorios (*)\n" +
            "• Selecciona una categoría principal\n" +
            "• Puedes elegir múltiples subcategorías\n" +
            "• El precio debe ser en colones por hora\n" +
            "• Describe claramente tu disponibilidad", "OK");
    }

    private async void OnReintentarClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("🔄 Reintentando cargar datos");
        CargarDatosIniciales();
    }

    #endregion

    #region Manejo de Categorías

    private void OnCategoriaSelectionChanged(object sender, EventArgs e)
    {
        try
        {
            if (CategoriaPicker.SelectedIndex >= 0 && CategoriaPicker.SelectedIndex < _categorias.Count)
            {
                var categoriaSeleccionada = _categorias[CategoriaPicker.SelectedIndex];
                System.Diagnostics.Debug.WriteLine($"📂 Categoría seleccionada: {categoriaSeleccionada.Nombre} (ID: {categoriaSeleccionada.CategoriaId})");

                // Filtrar subcategorías por categoría seleccionada
                _subCategoriasFiltradas = _subCategorias
                    .Where(sc => sc.Categoria?.CategoriaId == categoriaSeleccionada.CategoriaId)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"📋 SubCategorías filtradas: {_subCategoriasFiltradas.Count}");

                // Limpiar selecciones anteriores
                _subCategoriasSeleccionadas.Clear();

                // Cargar subcategorías en el contenedor
                CargarSubCategoriasUI();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("📂 Categoría deseleccionada");
                // Resetear subcategorías
                LimpiarSubCategorias();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en selección de categoría: {ex.Message}");
            DisplayAlert("Error", $"Error al cargar subcategorías: {ex.Message}", "OK");
        }
    }

    private void CargarSubCategoriasUI()
    {
        try
        {
            SubCategoriasContainer.Children.Clear();

            if (!_subCategoriasFiltradas.Any())
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

            foreach (var subCategoria in _subCategoriasFiltradas)
            {
                var checkboxStack = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 10,
                    Margin = new Thickness(0, 5)
                };

                var checkbox = new CheckBox
                {
                    IsChecked = false
                };

                var label = new Label
                {
                    Text = subCategoria.Nombre,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 14
                };

                // Evento para manejar selección
                checkbox.CheckedChanged += (s, e) => OnSubCategoriaCheckedChanged(subCategoria, e.Value);

                checkboxStack.Children.Add(checkbox);
                checkboxStack.Children.Add(label);

                SubCategoriasContainer.Children.Add(checkboxStack);
            }

            System.Diagnostics.Debug.WriteLine($"✅ UI de subcategorías creada: {_subCategoriasFiltradas.Count} elementos");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error creando UI subcategorías: {ex.Message}");
            DisplayAlert("Error", $"Error creando UI de subcategorías: {ex.Message}", "OK");
        }
    }

    private void OnSubCategoriaCheckedChanged(SubCategoria subCategoria, bool isChecked)
    {
        try
        {
            if (isChecked)
            {
                // Agregar a seleccionadas
                if (!_subCategoriasSeleccionadas.Any(s => s.SubCategoriaId == subCategoria.SubCategoriaId))
                {
                    _subCategoriasSeleccionadas.Add(new SubCategoriaSeleccionada
                    {
                        SubCategoriaId = subCategoria.SubCategoriaId,
                        Nombre = subCategoria.Nombre,
                        IsSelected = true,
                        CategoriaId = subCategoria.Categoria?.CategoriaId ?? 0
                    });
                    System.Diagnostics.Debug.WriteLine($"➕ SubCategoría agregada: {subCategoria.Nombre}");
                }
            }
            else
            {
                // Remover de seleccionadas
                _subCategoriasSeleccionadas.RemoveAll(s => s.SubCategoriaId == subCategoria.SubCategoriaId);
                System.Diagnostics.Debug.WriteLine($"➖ SubCategoría removida: {subCategoria.Nombre}");
            }

            System.Diagnostics.Debug.WriteLine($"📋 Total subcategorías seleccionadas: {_subCategoriasSeleccionadas.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en selección subcategoría: {ex.Message}");
            DisplayAlert("Error", $"Error al manejar selección: {ex.Message}", "OK");
        }
    }

    private void LimpiarSubCategorias()
    {
        SubCategoriasContainer.Children.Clear();
        _subCategoriasSeleccionadas.Clear();
        SubCategoriasContainer.Children.Add(new Label
        {
            Text = "Selecciona una categoría principal para ver las subcategorías",
            TextColor = Colors.Gray,
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });
        System.Diagnostics.Debug.WriteLine("🧹 SubCategorías limpiadas");
    }

    #endregion

    #region Validaciones

    private bool ValidarFormulario()
    {
        var errores = new List<string>();

        // Validar campos obligatorios
        if (string.IsNullOrWhiteSpace(TituloEntry.Text))
            errores.Add("El título es obligatorio");

        if (string.IsNullOrWhiteSpace(DescripcionEditor.Text))
            errores.Add("La descripción es obligatoria");

        if (string.IsNullOrWhiteSpace(PrecioEntry.Text))
            errores.Add("El precio es obligatorio");
        else if (!decimal.TryParse(PrecioEntry.Text, out decimal precio) || precio <= 0)
            errores.Add("El precio debe ser un número válido mayor a 0");

        if (string.IsNullOrWhiteSpace(DisponibilidadEntry.Text))
            errores.Add("La disponibilidad es obligatoria");

        if (CategoriaPicker.SelectedIndex < 0)
            errores.Add("Debe seleccionar una categoría");

        // Log de validación
        if (errores.Any())
        {
            System.Diagnostics.Debug.WriteLine($"❌ Errores de validación: {string.Join(", ", errores)}");
            DisplayAlert("Errores de validación", string.Join("\n", errores), "OK");
            return false;
        }

        System.Diagnostics.Debug.WriteLine("✅ Formulario validado correctamente");
        return true;
    }

    #endregion

    #region Publicar Servicio

    private async void OnPublicarClicked(object sender, EventArgs e)
    {
        // ========== LOGS DE DIAGNÓSTICO ==========
        System.Diagnostics.Debug.WriteLine("🔥🔥🔥 INICIANDO PUBLICAR SERVICIO 🔥🔥🔥");

        // 1. Verificar información de sesión
        var sessionId = SessionManager.ObtenerSessionId();
        var token = SessionManager.ObtenerToken();
        var userEmail = SessionManager.ObtenerEmailUsuario();
        var isLoggedIn = SessionManager.EstaLogueado();
        var tokenExpired = SessionManager.TokenExpirado();

        System.Diagnostics.Debug.WriteLine($"📱 SessionId: {sessionId}");
        System.Diagnostics.Debug.WriteLine($"📧 Email: {userEmail}");
        System.Diagnostics.Debug.WriteLine($"✅ Logueado: {isLoggedIn}");
        System.Diagnostics.Debug.WriteLine($"⏰ Token expirado: {tokenExpired}");
        System.Diagnostics.Debug.WriteLine($"🔑 Token existe: {!string.IsNullOrEmpty(token)}");
        if (!string.IsNullOrEmpty(token))
        {
            System.Diagnostics.Debug.WriteLine($"🔑 Token (primeros 50 chars): {token.Substring(0, Math.Min(50, token.Length))}...");
        }

        // 2. Verificar si es GUID válido
        if (Guid.TryParse(sessionId, out Guid parsedGuid))
        {
            System.Diagnostics.Debug.WriteLine($"✅ SessionId es GUID válido: {parsedGuid}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"❌ SessionId NO es GUID válido: {sessionId}");
        }

        // 3. Mostrar info también en pantalla para confirmar
        await DisplayAlert("Debug Info",
            $"SessionId: {(!string.IsNullOrEmpty(sessionId) ? "✅" : "❌")}\n" +
            $"Token: {(!string.IsNullOrEmpty(token) ? "✅" : "❌")}\n" +
            $"Logueado: {(isLoggedIn ? "✅" : "❌")}\n" +
            $"Token expirado: {(tokenExpired ? "❌" : "✅")}", "Continuar");

        System.Diagnostics.Debug.WriteLine("========================================");

        // Si algo falla, detener aquí para ver el problema
        if (!isLoggedIn || string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(token))
        {
            System.Diagnostics.Debug.WriteLine("❌ PROBLEMA DETECTADO: Falta sesión, token o sessionId");
            await DisplayAlert("Error de Sesión",
                "Problema detectado:\n" +
                $"- Logueado: {isLoggedIn}\n" +
                $"- SessionId: {!string.IsNullOrEmpty(sessionId)}\n" +
                $"- Token: {!string.IsNullOrEmpty(token)}", "OK");
            return;
        }

        if (!ValidarFormulario())
            return;

        try
        {
            // Verificar sesión nuevamente
            if (!SessionManager.EstaLogueado())
            {
                System.Diagnostics.Debug.WriteLine("❌ Sesión expirada durante validación");
                await DisplayAlert("Sesión requerida", "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Mostrar indicador de carga
            var button = sender as Button;
            var originalText = button.Text;
            button.Text = "Publicando...";
            button.IsEnabled = false;

            // Obtener categoría seleccionada
            var categoriaSeleccionada = _categorias[CategoriaPicker.SelectedIndex];
            System.Diagnostics.Debug.WriteLine($"📂 Categoría seleccionada: {categoriaSeleccionada.Nombre} (ID: {categoriaSeleccionada.CategoriaId})");

            // Convertir subcategorías seleccionadas al formato correcto
            var subCategoriasParaEnviar = _subCategoriasSeleccionadas.Select(sc =>
            {
                var subCategoriaOriginal = _subCategoriasFiltradas.FirstOrDefault(x => x.SubCategoriaId == sc.SubCategoriaId);
                return subCategoriaOriginal ?? new SubCategoria
                {
                    SubCategoriaId = sc.SubCategoriaId,
                    Nombre = sc.Nombre,
                    Categoria = categoriaSeleccionada
                };
            }).ToList();

            System.Diagnostics.Debug.WriteLine($"📋 SubCategorías para enviar: {subCategoriasParaEnviar.Count}");

            // CREAR EL REQUEST SEGÚN LA DOCUMENTACIÓN DEL API
            var request = new ReqInsertarServicio
            {
                SesionId = sessionId, // Usar el sessionId ya verificado
                Servicio = new Servicio
                {
                    ServicioId = 0, // Para nuevos servicios
                    Usuario = null, // El servidor lo obtendrá de la sesión
                    Categoria = categoriaSeleccionada,
                    Titulo = TituloEntry.Text.Trim(),
                    Descripcion = DescripcionEditor.Text.Trim(),
                    Precio = decimal.Parse(PrecioEntry.Text.Trim()),
                    Disponibilidad = DisponibilidadEntry.Text.Trim(),
                    SubCategorias = subCategoriasParaEnviar,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            };

            // Log del request completo
            System.Diagnostics.Debug.WriteLine($"=== REQUEST PARA API ===");
            System.Diagnostics.Debug.WriteLine($"SesionId: {request.SesionId}");
            System.Diagnostics.Debug.WriteLine($"Título: {request.Servicio.Titulo}");
            System.Diagnostics.Debug.WriteLine($"Descripción: {request.Servicio.Descripcion.Substring(0, Math.Min(50, request.Servicio.Descripcion.Length))}...");
            System.Diagnostics.Debug.WriteLine($"Precio: {request.Servicio.Precio}");
            System.Diagnostics.Debug.WriteLine($"Disponibilidad: {request.Servicio.Disponibilidad}");
            System.Diagnostics.Debug.WriteLine($"Categoría ID: {request.Servicio.Categoria?.CategoriaId}");
            System.Diagnostics.Debug.WriteLine($"Categoría Nombre: {request.Servicio.Categoria?.Nombre}");
            System.Diagnostics.Debug.WriteLine($"SubCategorías Count: {request.Servicio.SubCategorias?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine("=======================");

            // Verificar que el ApiService tenga el JwtHttpHandler
            System.Diagnostics.Debug.WriteLine("🌐 Llamando al API...");

            // Llamar al API
            var response = await _apiService.CrearServicioAsync(request);

            System.Diagnostics.Debug.WriteLine($"📡 Respuesta recibida - Resultado: {response.Resultado}");

            if (response.Resultado)
            {
                System.Diagnostics.Debug.WriteLine($"✅ ÉXITO - ServicioId: {response.ServicioId}, Mensaje: {response.Mensaje}");
                await DisplayAlert("¡Éxito!",
                    $"Servicio publicado exitosamente.\nID: {response.ServicioId}\n{response.Mensaje}",
                    "OK");

                // Limpiar formulario
                LimpiarFormulario();

                // Opcional: Navegar de vuelta
                await Navigation.PopAsync();
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido al publicar servicio";
                System.Diagnostics.Debug.WriteLine($"❌ ERROR API: {errorMessage}");

                // Log de todos los errores
                if (response.Error?.Any() == true)
                {
                    foreach (var error in response.Error)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error {error.ErrorCode}: {error.Message}");
                    }
                }

                await DisplayAlert("Error", $"No se pudo publicar el servicio:\n{errorMessage}", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 EXCEPCIÓN: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");
            await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar botón
            if (sender is Button btn)
            {
                btn.Text = "Publicar Servicio";
                btn.IsEnabled = true;
            }

            System.Diagnostics.Debug.WriteLine("🏁 FIN DE PUBLICAR SERVICIO");
        }
    }

    private async void OnGuardarBorradorClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("💾 Solicitud de guardar borrador");
        await DisplayAlert("Guardar Borrador", "Funcionalidad de guardar borrador próximamente", "OK");
    }

    #endregion

    #region Utilidades

    private void LimpiarFormulario()
    {
        try
        {
            TituloEntry.Text = "";
            DescripcionEditor.Text = "";
            PrecioEntry.Text = "";
            DisponibilidadEntry.Text = "";
            CategoriaPicker.SelectedIndex = -1;
            LimpiarSubCategorias();
            System.Diagnostics.Debug.WriteLine("🧹 Formulario limpiado");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error limpiando formulario: {ex.Message}");
        }
    }

    #endregion

    #region Cleanup

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _apiService?.Dispose();
        System.Diagnostics.Debug.WriteLine("🚪 PublicarServicioPage cerrada");
    }

    #endregion
}