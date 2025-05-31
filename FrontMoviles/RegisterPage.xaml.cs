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
    private List<Distrito> _distritos;

    // Propiedades para el binding de fechas
    public DateTime MaxDate => DateTime.Now.AddYears(-18);
    public DateTime MinDate => DateTime.Now.AddYears(-100);

    public RegisterPage()
    {
        InitializeComponent();
        _apiService = new ApiService();

        // Establecer el BindingContext para las fechas
        BindingContext = this;

        // Configurar fechas
        ConfigurarFechas();

        // Cargar provincias
        CargarProvincias();
    }

    #region Configuración Inicial

    private void ConfigurarFechas()
    {
        // Configurar fecha de nacimiento
        FechaNacimientoPicker.MaximumDate = DateTime.Now.AddYears(-18); // Mínimo 18 años
        FechaNacimientoPicker.MinimumDate = DateTime.Now.AddYears(-100); // Máximo 100 años
        FechaNacimientoPicker.Date = DateTime.Now.AddYears(-25); // Fecha por defecto
    }

    private async void CargarProvincias()
    {
        try
        {
            _provincias = await _apiService.ObtenerProvinciasAsync();

            ProvinciaPicker.ItemsSource = _provincias;
            ProvinciaPicker.ItemDisplayBinding = new Binding("Nombre");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al cargar provincias: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Eventos de Selección

    private async void OnProvinciaSelectionChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        if (picker.SelectedItem is Provincia provinciaSeleccionada)
        {
            try
            {
                // Limpiar cantones y distritos
                CantonPicker.ItemsSource = null;
                DistritoPicker.ItemsSource = null;
                CantonPicker.IsEnabled = false;
                DistritoPicker.IsEnabled = false;

                // Cargar cantones
                _cantones = await _apiService.ObtenerCantonesPorProvinciaAsync(provinciaSeleccionada.ProvinciaId);

                if (_cantones.Any())
                {
                    CantonPicker.ItemsSource = _cantones;
                    CantonPicker.ItemDisplayBinding = new Binding("Nombre");
                    CantonPicker.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar cantones: {ex.Message}", "OK");
            }
        }
    }

    private async void OnCantonSelectionChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        if (picker.SelectedItem is Canton cantonSeleccionado)
        {
            try
            {
                // Limpiar distritos
                DistritoPicker.ItemsSource = null;
                DistritoPicker.IsEnabled = false;

                // Cargar distritos
                _distritos = await _apiService.ObtenerDistritosPorCantonAsync(cantonSeleccionado.CantonId);

                if (_distritos.Any())
                {
                    DistritoPicker.ItemsSource = _distritos;
                    DistritoPicker.ItemDisplayBinding = new Binding("Nombre");
                    DistritoPicker.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al cargar distritos: {ex.Message}", "OK");
            }
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
        if (ProvinciaPicker.SelectedItem == null)
        {
            errores.Add("Debes seleccionar una provincia");
            esValido = false;
        }

        if (CantonPicker.SelectedItem == null)
        {
            errores.Add("Debes seleccionar un cantón");
            esValido = false;
        }

        if (DistritoPicker.SelectedItem == null)
        {
            errores.Add("Debes seleccionar un distrito");
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

            // Obtener los objetos seleccionados
            var provinciaSeleccionada = ProvinciaPicker.SelectedItem as Provincia;
            var cantonSeleccionado = CantonPicker.SelectedItem as Canton;
            var distritoSeleccionado = DistritoPicker.SelectedItem as Distrito;

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

                    // Ubicación seleccionada
                    Provincia = provinciaSeleccionada,
                    Canton = cantonSeleccionado,
                    Distrito = distritoSeleccionado,

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