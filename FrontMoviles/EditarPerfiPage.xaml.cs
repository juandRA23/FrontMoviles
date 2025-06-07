using FrontMoviles.Servicios;
using FrontMoviles.Modelos;
using System.Text.RegularExpressions;

namespace FrontMoviles;

public partial class EditarPerfilPage : ContentPage
{
    private readonly ApiService _apiService;
    private Usuario _usuarioActual;
    private List<Provincia> _provincias = new List<Provincia>();
    private List<Canton> _cantones = new List<Canton>();
    private List<Canton> _cantonesFiltrados = new List<Canton>();
    private bool _datosModificados = false;

    public EditarPerfilPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        CargarDatosIniciales();
    }

    #region Configuración inicial

    private async void CargarDatosIniciales()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🔄 === CARGANDO EDITAR PERFIL ===");

            // Mostrar loading
            MostrarEstado("loading");

            // Verificar sesión
            if (!SessionManager.EstaLogueado())
            {
                System.Diagnostics.Debug.WriteLine("❌ No hay sesión activa en EditarPerfil");
                await MostrarErrorSesionYRedirigir();
                return;
            }

            // Cargar datos en paralelo
            var taskPerfil = _apiService.ObtenerPerfilUsuarioAsync();
            var taskProvincias = _apiService.ObtenerProvinciasAsync();
            var taskCantones = _apiService.ObtenerCantonesAsync();

            await Task.WhenAll(taskPerfil, taskProvincias, taskCantones);

            var responsePerfil = await taskPerfil;
            var responseProvincias = await taskProvincias;
            var responseCantones = await taskCantones;

            // Procesar perfil
            if (responsePerfil.Resultado && responsePerfil.Usuario != null)
            {
                _usuarioActual = responsePerfil.Usuario;
                System.Diagnostics.Debug.WriteLine($"✅ Perfil cargado: {_usuarioActual.Nombre} {_usuarioActual.Apellido1}");
            }
            else
            {
                var errorMsg = responsePerfil.Error?.FirstOrDefault()?.Message ?? "Error cargando perfil";
                System.Diagnostics.Debug.WriteLine($"❌ Error cargando perfil: {errorMsg}");

                if (errorMsg.Contains("sesión") || errorMsg.Contains("inválida"))
                {
                    await MostrarErrorSesionYRedirigir();
                    return;
                }

                MostrarError($"Error al cargar perfil: {errorMsg}");
                return;
            }

            // Procesar provincias
            if (responseProvincias.Resultado && responseProvincias.Provincias?.Any() == true)
            {
                _provincias = responseProvincias.Provincias;
                System.Diagnostics.Debug.WriteLine($"✅ Provincias cargadas: {_provincias.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Error cargando provincias, usando datos de respaldo");
                CargarProvinciasRespaldo();
            }

            // Procesar cantones
            if (responseCantones.Resultado && responseCantones.Cantones?.Any() == true)
            {
                _cantones = responseCantones.Cantones;
                System.Diagnostics.Debug.WriteLine($"✅ Cantones cargados: {_cantones.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Error cargando cantones, usando datos de respaldo");
                CargarCantonesRespaldo();
            }

            // Cargar datos en UI
            CargarDatosEnUI();
            MostrarEstado("content");

            System.Diagnostics.Debug.WriteLine("✅ EditarPerfil cargado correctamente");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Error en CargarDatosIniciales: {ex.Message}");
            MostrarError($"Error inesperado: {ex.Message}");
        }
    }

    private void CargarDatosEnUI()
    {
        try
        {
            // Información no editable
            var nombreCompleto = $"{_usuarioActual.Nombre} {_usuarioActual.Apellido1}";
            if (!string.IsNullOrEmpty(_usuarioActual.Apellido2))
            {
                nombreCompleto += $" {_usuarioActual.Apellido2}";
            }
            NombreCompletoLabel.Text = nombreCompleto;
            EmailLabel.Text = _usuarioActual.Correo;

            // Información editable
            TelefonoEntry.Text = _usuarioActual.Telefono ?? "";
            DireccionEditor.Text = _usuarioActual.Direccion ?? "";

            // Cargar provincias en picker
            CargarProvinciasPicker();

            // Seleccionar provincia actual
            if (_usuarioActual.Provincia != null)
            {
                var provinciaIndex = _provincias.FindIndex(p => p.ProvinciaId == _usuarioActual.Provincia.ProvinciaId);
                if (provinciaIndex >= 0)
                {
                    ProvinciaPicker.SelectedIndex = provinciaIndex;

                    // Cargar cantones de esa provincia
                    CargarCantonesPorProvincia(_usuarioActual.Provincia.ProvinciaId);

                    // Seleccionar cantón actual
                    if (_usuarioActual.Canton != null)
                    {
                        var cantonIndex = _cantonesFiltrados.FindIndex(c => c.CantonId == _usuarioActual.Canton.CantonId);
                        if (cantonIndex >= 0)
                        {
                            CantonPicker.SelectedIndex = cantonIndex;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("✅ Datos cargados en UI");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando datos en UI: {ex.Message}");
            DisplayAlert("Error", $"Error al mostrar datos: {ex.Message}", "OK");
        }
    }

    private void CargarProvinciasPicker()
    {
        try
        {
            ProvinciaPicker.ItemsSource = null;
            ProvinciaPicker.ItemsSource = _provincias.Select(p => p.Nombre).ToList();
            ProvinciaPicker.Title = "Seleccionar provincia";
            System.Diagnostics.Debug.WriteLine("✅ Picker de provincias configurado");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error configurando picker: {ex.Message}");
        }
    }

    private void CargarCantonesPorProvincia(int provinciaId)
    {
        try
        {
            _cantonesFiltrados = _cantones.Where(c => c.Provincia?.ProvinciaId == provinciaId).ToList();

            CantonPicker.ItemsSource = null;
            CantonPicker.ItemsSource = _cantonesFiltrados.Select(c => c.Nombre).ToList();
            CantonPicker.SelectedIndex = -1;
            CantonPicker.IsEnabled = _cantonesFiltrados.Any();
            CantonPicker.Title = _cantonesFiltrados.Any() ? "Seleccionar cantón" : "No hay cantones disponibles";

            System.Diagnostics.Debug.WriteLine($"✅ Cantones cargados para provincia {provinciaId}: {_cantonesFiltrados.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cargando cantones: {ex.Message}");
        }
    }

    private void CargarProvinciasRespaldo()
    {
        _provincias = new List<Provincia>
        {
            new Provincia { ProvinciaId = 1, Nombre = "San José" },
            new Provincia { ProvinciaId = 2, Nombre = "Alajuela" },
            new Provincia { ProvinciaId = 3, Nombre = "Cartago" },
            new Provincia { ProvinciaId = 4, Nombre = "Heredia" },
            new Provincia { ProvinciaId = 5, Nombre = "Guanacaste" },
            new Provincia { ProvinciaId = 6, Nombre = "Puntarenas" },
            new Provincia { ProvinciaId = 7, Nombre = "Limón" }
        };
    }

    private void CargarCantonesRespaldo()
    {
        _cantones = new List<Canton>
        {
            new Canton { CantonId = 1, Nombre = "San José", Provincia = new Provincia { ProvinciaId = 1, Nombre = "San José" } },
            new Canton { CantonId = 2, Nombre = "Escazú", Provincia = new Provincia { ProvinciaId = 1, Nombre = "San José" } },
            new Canton { CantonId = 3, Nombre = "Desamparados", Provincia = new Provincia { ProvinciaId = 1, Nombre = "San José" } },
            new Canton { CantonId = 4, Nombre = "Alajuela", Provincia = new Provincia { ProvinciaId = 2, Nombre = "Alajuela" } },
            new Canton { CantonId = 5, Nombre = "San Ramón", Provincia = new Provincia { ProvinciaId = 2, Nombre = "Alajuela" } }
        };
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

    #region Eventos de UI

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (_datosModificados)
        {
            bool salir = await DisplayAlert(
                "Cambios sin guardar",
                "Tienes cambios sin guardar. ¿Estás seguro que deseas salir?",
                "Sí, salir",
                "Continuar editando");

            if (!salir) return;
        }

        await Navigation.PopAsync();
    }

    private async void OnHelpClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Ayuda - Editar Perfil",
            "• Solo puedes editar ciertos campos de tu perfil\n" +
            "• El nombre y email no se pueden cambiar\n" +
            "• Asegúrate de que tu teléfono esté correcto\n" +
            "• Selecciona tu ubicación actual\n" +
            "• Incluye referencias en la dirección", "OK");
    }

    private async void OnReintentarClicked(object sender, EventArgs e)
    {
        CargarDatosIniciales();
    }

    #endregion

    #region Eventos de provincia y cantón

    private void OnProvinciaSelectionChanged(object sender, EventArgs e)
    {
        try
        {
            if (ProvinciaPicker.SelectedIndex >= 0 && ProvinciaPicker.SelectedIndex < _provincias.Count)
            {
                var provinciaSeleccionada = _provincias[ProvinciaPicker.SelectedIndex];
                System.Diagnostics.Debug.WriteLine($"📍 Provincia seleccionada: {provinciaSeleccionada.Nombre}");

                CargarCantonesPorProvincia(provinciaSeleccionada.ProvinciaId);
                _datosModificados = true;
            }
            else
            {
                CantonPicker.ItemsSource = null;
                CantonPicker.IsEnabled = false;
                CantonPicker.Title = "Seleccionar cantón";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en selección de provincia: {ex.Message}");
            DisplayAlert("Error", $"Error al cargar cantones: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Eventos de datos modificados

    private void OnTelefonoTextChanged(object sender, TextChangedEventArgs e)
    {
        _datosModificados = true;
    }

    private void OnDireccionTextChanged(object sender, TextChangedEventArgs e)
    {
        _datosModificados = true;
    }

    private void OnCantonSelectionChanged(object sender, EventArgs e)
    {
        _datosModificados = true;
    }

    #endregion

    #region Validaciones

    private bool ValidarFormulario()
    {
        var errores = new List<string>();

        // Validar teléfono
        if (string.IsNullOrWhiteSpace(TelefonoEntry.Text))
        {
            errores.Add("El teléfono es obligatorio");
        }
        else if (!ValidarTelefono(TelefonoEntry.Text))
        {
            errores.Add("El formato del teléfono no es válido");
        }

        // Validar provincia
        if (ProvinciaPicker.SelectedIndex < 0)
        {
            errores.Add("Debe seleccionar una provincia");
        }

        // Validar cantón
        if (CantonPicker.SelectedIndex < 0)
        {
            errores.Add("Debe seleccionar un cantón");
        }

        if (errores.Any())
        {
            System.Diagnostics.Debug.WriteLine($"❌ Errores de validación: {string.Join(", ", errores)}");
            DisplayAlert("Errores de validación", string.Join("\n", errores), "OK");
            return false;
        }

        System.Diagnostics.Debug.WriteLine("✅ Formulario validado correctamente");
        return true;
    }

    private bool ValidarTelefono(string telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono))
            return false;

        // Remover espacios y caracteres especiales
        var numeroLimpio = telefono.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");

        // Validar que tenga 8 dígitos (Costa Rica) o 11 con código de país
        return numeroLimpio.Length == 8 || (numeroLimpio.StartsWith("506") && numeroLimpio.Length == 11);
    }

    #endregion

    #region Guardar cambios

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (!ValidarFormulario())
            return;

        try
        {
            System.Diagnostics.Debug.WriteLine("💾 === GUARDANDO CAMBIOS ===");

            // Mostrar indicador de carga
            var button = sender as Button;
            var originalText = button?.Text;
            if (button != null)
            {
                button.Text = "Guardando...";
                button.IsEnabled = false;
            }

            // Verificar sesión
            if (!SessionManager.EstaLogueado())
            {
                await MostrarErrorSesionYRedirigir();
                return;
            }

            // Obtener datos seleccionados
            var provinciaSeleccionada = _provincias[ProvinciaPicker.SelectedIndex];
            var cantonSeleccionado = _cantonesFiltrados[CantonPicker.SelectedIndex];

            // Crear request
            var request = new ReqActualizarDatosUsuario
            {
                SesionId = SessionManager.ObtenerSessionId(),
                ProvinciaId = provinciaSeleccionada.ProvinciaId,
                CantonId = cantonSeleccionado.CantonId,
                Telefono = TelefonoEntry.Text.Trim(),
                Direccion = DireccionEditor.Text?.Trim() ?? ""
            };

            System.Diagnostics.Debug.WriteLine($"📤 Enviando actualización:");
            System.Diagnostics.Debug.WriteLine($"  - Provincia: {provinciaSeleccionada.Nombre} (ID: {provinciaSeleccionada.ProvinciaId})");
            System.Diagnostics.Debug.WriteLine($"  - Cantón: {cantonSeleccionado.Nombre} (ID: {cantonSeleccionado.CantonId})");
            System.Diagnostics.Debug.WriteLine($"  - Teléfono: {request.Telefono}");
            System.Diagnostics.Debug.WriteLine($"  - Dirección: {request.Direccion}");

            // Llamar al API
            var response = await _apiService.ActualizarDatosUsuarioAsync(request);

            if (response.Resultado)
            {
                System.Diagnostics.Debug.WriteLine("✅ ÉXITO - Datos actualizados");

                _datosModificados = false;

                await DisplayAlert("¡Éxito!", "Tus datos han sido actualizados correctamente", "OK");

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

                await DisplayAlert("Error", $"No se pudieron actualizar los datos:\n{errorMessage}", "OK");
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
                btn.Text = "Guardar cambios";
                btn.IsEnabled = true;
            }

            System.Diagnostics.Debug.WriteLine("🏁 FIN DE GUARDAR CAMBIOS");
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

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        if (_datosModificados)
        {
            bool confirmar = await DisplayAlert(
                "Cancelar cambios",
                "Tienes cambios sin guardar. ¿Estás seguro que deseas cancelar?",
                "Sí, cancelar",
                "Continuar editando");

            if (!confirmar) return;
        }

        await Navigation.PopAsync();
    }

    #endregion

    #region Estados de UI

    private void MostrarEstado(string estado)
    {
        LoadingGrid.IsVisible = estado == "loading";
        ContentScrollView.IsVisible = estado == "content";
        ErrorGrid.IsVisible = estado == "error";
        System.Diagnostics.Debug.WriteLine($"🔄 Estado UI: {estado}");
    }

    private void MostrarError(string mensaje)
    {
        ErrorMessageLabel.Text = mensaje;
        MostrarEstado("error");
        System.Diagnostics.Debug.WriteLine($"❌ Error mostrado: {mensaje}");
    }

    #endregion

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Agregar eventos de cambio de texto
        TelefonoEntry.TextChanged += OnTelefonoTextChanged;
        DireccionEditor.TextChanged += OnDireccionTextChanged;
        CantonPicker.SelectedIndexChanged += OnCantonSelectionChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Remover eventos
        TelefonoEntry.TextChanged -= OnTelefonoTextChanged;
        DireccionEditor.TextChanged -= OnDireccionTextChanged;
        CantonPicker.SelectedIndexChanged -= OnCantonSelectionChanged;

        _apiService?.Dispose();
        System.Diagnostics.Debug.WriteLine("🚪 EditarPerfilPage cerrada");
    }

    #endregion
}