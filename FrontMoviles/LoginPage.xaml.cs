using FrontMoviles.Modelos;
using FrontMoviles.Servicios;
using System.Text.RegularExpressions;

namespace FrontMoviles;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private bool _isPasswordVisible = false;

    public LoginPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    #region Validaciones

    private bool ValidarFormulario()
    {
        bool esValido = true;
        var errores = new List<string>();

        // Validar email (obligatorio y formato)
        if (string.IsNullOrWhiteSpace(EmailEntry.Text))
        {
            errores.Add("El correo electrónico es obligatorio");
            esValido = false;
        }
        else if (!ValidarEmail(EmailEntry.Text))
        {
            errores.Add("El formato del correo electrónico no es válido");
            esValido = false;
        }

        // Validar contraseña (obligatorio)
        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            errores.Add("La contraseña es obligatoria");
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

    #endregion

    #region Eventos de UI

    private void OnShowPasswordTapped(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        PasswordEntry.IsPassword = !_isPasswordVisible;

        if (sender is Label label)
        {
            label.Text = _isPasswordVisible ? "Ocultar" : "Mostrar";
        }
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Recuperar Contraseña", "Funcionalidad de recuperación de contraseña próximamente.", "OK");
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }

    #endregion

    #region Login de Usuario

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (!ValidarFormulario())
            return;

        try
        {
            // Mostrar indicador de carga
            var button = sender as Button;
            button.Text = "Iniciando sesión...";
            button.IsEnabled = false;

            // Crear el objeto de request
            var request = new ReqLoginUsuario
            {
                Usuario = new UsuarioLogin
                {
                    Correo = EmailEntry.Text?.Trim().ToLower(),
                    Contrasena = PasswordEntry.Text
                }
            };

            // Llamar al API
            var response = await _apiService.LoginUsuarioAsync(request);

            // Procesar respuesta
            if (response.Resultado && response.Sesion != null)
            {
                // Login exitoso - Guardar sesión
                SessionManager.GuardarSesion(response.Sesion, EmailEntry.Text?.Trim().ToLower());

                await DisplayAlert("Éxito", "Inicio de sesión exitoso", "OK");

                // Navegar a la página principal
                //Application.Current.MainPage = new AppShell();

                // O si quieres navegar a una página específica:
                 await Navigation.PushAsync(new InicioPage());
            }
            else
            {
                // Mostrar errores del servidor
                var mensajesError = response.Error?.Select(e => e.Message) ?? new[] { "Credenciales incorrectas" };
                var mensaje = string.Join("\n", mensajesError);
                await DisplayAlert("Error de inicio de sesión", mensaje, "OK");
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
                button.Text = "Iniciar sesión";
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