using FrontMoviles.Modelos;
using FrontMoviles.Servicios;
using System.Text.RegularExpressions;

namespace FrontMoviles;

public partial class RegisterPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isPasswordVisible = false;
    private List<Provincia> _provincias;
    private List<Canton> _cantones;
    private List<Canton> _cantonesFiltrados;

    // Propiedades para el binding de fechas
    public DateTime MaxDate => DateTime.Now.AddYears(-18);
    public DateTime MinDate => DateTime.Now.AddYears(-100);

    public RegisterPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _provincias = new List<Provincia>();
        _cantones = new List<Canton>();
        _cantonesFiltrados = new List<Canton>();

        // Establecer el BindingContext para las fechas
        BindingContext = this;

        // Configurar fechas
        ConfigurarFechas();

        // Cargar datos de ubicación
        CargarDatosUbicacion();
    }

    #region Configuración Inicial

    private void ConfigurarFechas()
    {
        // Configurar fecha de nacimiento
        FechaNacimientoPicker.MaximumDate = DateTime.Now.AddYears(-18); // Mínimo 18 años
        FechaNacimientoPicker.MinimumDate = DateTime.Now.AddYears(-100); // Máximo 100 años
        FechaNacimientoPicker.Date = DateTime.Now.AddYears(-25); // Fecha por defecto
    }

    private async void CargarDatosUbicacion()
    {
        try
        {
            // Cargar provincias y cantones en paralelo
            var provinciasTask = _apiService.ObtenerProvinciasAsync();
            var cantonesTask = _apiService.ObtenerCantonesAsync();

            await Task.WhenAll(provinciasTask, cantonesTask);

            var provinciasResponse = await provinciasTask;
            var cantonesResponse = await cantonesTask;

            // Verificar respuestas
            if (provinciasResponse.Resultado)
            {
                _provincias = provinciasResponse.Provincias ?? new List<Provincia>();
                CargarProvinciasEnPicker();
            }
            else
            {
                // Si falla, usar datos mock
                _provincias = GetMockProvincias();
                CargarProvinciasEnPicker();
            }

            if (cantonesResponse.Resultado)
            {
                _cantones = cantonesResponse.Cantones ?? new List<Canton>();
            }
            else
            {
                // Si falla, usar datos mock
                _cantones = GetMockCantones();
            }
        }
        catch (Exception ex)
        {
            // En caso de error, usar datos mock
            _provincias = GetMockProvincias();
            _cantones = GetMockCantones();
            CargarProvinciasEnPicker();

            await DisplayAlert("Advertencia", "Error al cargar ubicaciones. Usando datos locales.", "OK");
        }
    }

    private void CargarProvinciasEnPicker()
    {
        ProvinciaPicker.ItemsSource = null;
        if (_provincias.Any())
        {
            var provinciasNombres = _provincias.Select(p => p.Nombre).ToList();
            ProvinciaPicker.ItemsSource = provinciasNombres;
        }
    }

    private List<Provincia> GetMockProvincias()
    {
        var provincias = new List<Provincia>();
        provincias.Add(new Provincia { ProvinciaId = 1, Nombre = "San José" });
        provincias.Add(new Provincia { ProvinciaId = 2, Nombre = "Alajuela" });
        provincias.Add(new Provincia { ProvinciaId = 3, Nombre = "Cartago" });
        provincias.Add(new Provincia { ProvinciaId = 4, Nombre = "Heredia" });
        provincias.Add(new Provincia { ProvinciaId = 5, Nombre = "Guanacaste" });
        provincias.Add(new Provincia { ProvinciaId = 6, Nombre = "Puntarenas" });
        provincias.Add(new Provincia { ProvinciaId = 7, Nombre = "Limón" });
        return provincias;
    }

    private List<Canton> GetMockCantones()
    {
        var cantones = new List<Canton>();
        var sanJose = new Provincia { ProvinciaId = 1, Nombre = "San José" };

        cantones.Add(new Canton { CantonId = 1, Nombre = "San José", Provincia = sanJose });
        cantones.Add(new Canton { CantonId = 2, Nombre = "Escazú", Provincia = sanJose });
        cantones.Add(new Canton { CantonId = 3, Nombre = "Desamparados", Provincia = sanJose });
        cantones.Add(new Canton { CantonId = 4, Nombre = "Puriscal", Provincia = sanJose });
        cantones.Add(new Canton { CantonId = 5, Nombre = "Tarrazú", Provincia = sanJose });

        return cantones;
    }

    #endregion

    #region Eventos de Selección

    private void OnProvinciaSelectionChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        if (picker?.SelectedIndex >= 0 && picker.SelectedIndex < _provincias.Count)
        {
            var provinciaSeleccionada = _provincias[picker.SelectedIndex];
            CargarCantonesPorProvincia(provinciaSeleccionada.ProvinciaId);
        }
    }

    private void CargarCantonesPorProvincia(int provinciaId)
    {
        try
        {
            // Filtrar cantones por provincia seleccionada
            _cantonesFiltrados = _apiService.FiltrarCantonesPorProvincia(_cantones, provinciaId);

            CantonPicker.ItemsSource = null;
            CantonPicker.IsEnabled = false;

            if (_cantonesFiltrados.Any())
            {
                var cantonesNombres = _cantonesFiltrados.Select(c => c.Nombre).ToList();
                CantonPicker.ItemsSource = cantonesNombres;
                CantonPicker.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Error al cargar cantones: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Validaciones

    private bool ValidarFormulario()
    {
        bool esValido = true;
        var errores = new List<string>();

        // Validar nombre (obligatorio)
        if (string.IsNullOrWhiteSpace(NombreEntry.Text))
        {
            errores.Add("El nombre es obligatorio");
            esValido = false;
        }

        // Validar primer apellido (obligatorio)
        if (string.IsNullOrWhiteSpace(Apellido1Entry.Text))
        {
            errores.Add("El primer apellido es obligatorio");
            esValido = false;
        }

        // Validar teléfono (obligatorio)
        if (string.IsNullOrWhiteSpace(TelefonoEntry.Text))
        {
            errores.Add("El teléfono es obligatorio");
            esValido = false;
        }

        // Validar ubicación
        if (ProvinciaPicker.SelectedIndex < 0)
        {
            errores.Add("Debes seleccionar una provincia");
            esValido = false;
        }

        if (CantonPicker.SelectedIndex < 0)
        {
            errores.Add("Debes seleccionar un cantón");
            esValido = false;
        }

        // Validar email (obligatorio y formato)
        if (string.IsNullOrWhiteSpace(CorreoEntry.Text))
        {
            errores.Add("El correo electrónico es obligatorio");
            CorreoErrorLabel.IsVisible = true;
            esValido = false;
        }
        else if (!ValidarEmail(CorreoEntry.Text))
        {
            errores.Add("El formato del correo electrónico no es válido");
            CorreoErrorLabel.IsVisible = true;
            esValido = false;
        }
        else
        {
            CorreoErrorLabel.IsVisible = false;
        }

        // Validar contraseña (obligatorio y fortaleza)
        if (string.IsNullOrWhiteSpace(ContrasenaEntry.Text))
        {
            errores.Add("La contraseña es obligatoria");
            esValido = false;
        }
        else if (ContrasenaEntry.Text.Length < 8)
        {
            errores.Add("La contraseña debe tener al menos 8 caracteres");
            esValido = false;
        }

        // Validar confirmación de contraseña
        if (ContrasenaEntry.Text != ConfirmarContrasenaEntry.Text)
        {
            errores.Add("Las contraseñas no coinciden");
            ConfirmarErrorLabel.IsVisible = true;
            esValido = false;
        }
        else
        {
            ConfirmarErrorLabel.IsVisible = false;
        }

        // Validar términos y condiciones
        if (!TerminosCheck.IsChecked)
        {
            errores.Add("Debes aceptar los términos y condiciones");
            esValido = false;
        }

        // Mostrar errores si los hay
        if (!esValido)
        {
            DisplayAlert("Datos incompletos", string.Join("\n", errores), "OK");
        }

        return esValido;
    }

    private bool ValidarEmail(string email)
    {
        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailPattern);
    }

    private void ValidarFortalezaContrasena(string password)
    {
        int fortaleza = 0;

        if (password.Length >= 8) fortaleza++;
        if (Regex.IsMatch(password, @"[A-Z]")) fortaleza++;
        if (Regex.IsMatch(password, @"[0-9]")) fortaleza++;
        if (Regex.IsMatch(password, @"[^a-zA-Z0-9]")) fortaleza++;

        var colores = new Color[]
        {
            Color.FromArgb("#E0E0E0"),
            Color.FromArgb("#FF4444"),
            Color.FromArgb("#FFA500"),
            Color.FromArgb("#4CAF50")
        };

        // Calcular el índice de color de manera segura
        var colorIndex = fortaleza == 0 ? 0 : Math.Min(fortaleza, 3);

        // Actualizar barras de fortaleza
        StrengthBar1.Color = fortaleza >= 1 ? colores[Math.Min(fortaleza, 3)] : colores[0];
        StrengthBar2.Color = fortaleza >= 2 ? colores[Math.Min(fortaleza, 3)] : colores[0];
        StrengthBar3.Color = fortaleza >= 3 ? colores[Math.Min(fortaleza, 3)] : colores[0];
        StrengthBar4.Color = fortaleza >= 4 ? colores[3] : colores[0];

        // Actualizar texto
        var mensajes = new string[]
        {
            "La contraseña debe tener al menos 8 caracteres",
            "Contraseña débil",
            "Contraseña regular",
            "Contraseña buena",
            "Contraseña fuerte"
        };

        PasswordStrengthLabel.Text = mensajes[fortaleza];
        PasswordStrengthLabel.TextColor = colores[colorIndex == 0 ? 1 : colorIndex];
    }

    #endregion

    #region Eventos de UI

    private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.NewTextValue))
        {
            ValidarFortalezaContrasena(e.NewTextValue);
        }
    }

    private void OnShowPasswordTapped(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        ContrasenaEntry.IsPassword = !_isPasswordVisible;

        if (sender is Label label)
        {
            label.Text = _isPasswordVisible ? "Ocultar" : "Mostrar";
        }
    }

    private async void OnTermsClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Términos y Condiciones", "Aquí irían los términos y condiciones completos.", "OK");
    }

    private async void OnPrivacyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Política de Privacidad", "Aquí iría la política de privacidad completa.", "OK");
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync(); // Regresar a login
    }

    #endregion

    #region Registro de Usuario

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (!ValidarFormulario())
            return;

        try
        {
            // Mostrar indicador de carga
            var button = sender as Button;
            button.Text = "Registrando...";
            button.IsEnabled = false;

            // Obtener ubicación seleccionada
            Provincia provinciaSeleccionada = null;
            Canton cantonSeleccionado = null;

            if (ProvinciaPicker.SelectedIndex >= 0 && ProvinciaPicker.SelectedIndex < _provincias.Count)
            {
                provinciaSeleccionada = _provincias[ProvinciaPicker.SelectedIndex];
            }

            if (CantonPicker.SelectedIndex >= 0 && CantonPicker.SelectedIndex < _cantonesFiltrados.Count)
            {
                cantonSeleccionado = _cantonesFiltrados[CantonPicker.SelectedIndex];
            }

            // Crear el objeto de request
            var request = new ReqInsertarUsuario
            {
                Usuario = new Usuario
                {
                    Nombre = NombreEntry.Text?.Trim(),
                    Apellido1 = Apellido1Entry.Text?.Trim(),
                    Apellido2 = string.IsNullOrWhiteSpace(Apellido2Entry.Text) ? "" : Apellido2Entry.Text.Trim(),
                    FechaNacimiento = FechaNacimientoPicker.Date,
                    Correo = CorreoEntry.Text?.Trim().ToLower(),
                    Telefono = TelefonoEntry.Text?.Trim(),
                    Contrasena = ContrasenaEntry.Text,
                    Direccion = DireccionEntry.Text?.Trim() ?? "",
                    FotoPerfil = "",

                    // Ubicación seleccionada (sin distrito)
                    Provincia = provinciaSeleccionada,
                    Canton = cantonSeleccionado,


                    // Valores por defecto
                    UsuarioId = 0,
                    Salt = "",
                    Verificacion = 0,
                    Activo = true,
                    PerfilCompleto = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }
            };

            // Llamar al API
            var response = await _apiService.RegistrarUsuarioAsync(request);

            // Procesar respuesta
            if (response.Resultado)
            {
                await DisplayAlert("Éxito", "Usuario registrado exitosamente. Por favor verifica tu cuenta.", "OK");

                // Navegar a la página de verificación
                await Navigation.PushAsync(new VerificationPage(CorreoEntry.Text?.Trim().ToLower()));
            }
            else
            {
                // Mostrar errores del servidor
                var mensajesError = response.Error?.Select(e => e.Message) ?? new[] { "Error desconocido" };
                var mensaje = string.Join("\n", mensajesError);
                await DisplayAlert("Error", mensaje, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar botón
            if (sender is Button button)
            {
                button.Text = "Crear cuenta";
                button.IsEnabled = true;
            }
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