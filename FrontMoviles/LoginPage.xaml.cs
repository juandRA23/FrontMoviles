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
            errores.Add("El correo electr�nico es obligatorio");
            esValido = false;
        }
        else if (!ValidarEmail(EmailEntry.Text))
        {
            errores.Add("El formato del correo electr�nico no es v�lido");
            esValido = false;
        }

        // Validar contrase�a (obligatorio)
        if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            errores.Add("La contrase�a es obligatoria");
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
        await DisplayAlert("Recuperar Contrase�a", "Funcionalidad de recuperaci�n de contrase�a pr�ximamente.", "OK");
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
            button.Text = "Iniciando sesi�n...";
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
                // Login exitoso - Guardar sesi�n
                SessionManager.GuardarSesion(response.Sesion, EmailEntry.Text?.Trim().ToLower());

                await DisplayAlert("�xito", "Inicio de sesi�n exitoso", "OK");

                // Navegar a la p�gina principal
                //Application.Current.MainPage = new AppShell();

                // O si quieres navegar a una p�gina espec�fica:
                 await Navigation.PushAsync(new InicioPage());
            }
            else
            {
                // Mostrar errores del servidor
                var mensajesError = response.Error?.Select(e => e.Message) ?? new[] { "Credenciales incorrectas" };
                var mensaje = string.Join("\n", mensajesError);
                await DisplayAlert("Error de inicio de sesi�n", mensaje, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error inesperado: {ex.Message}", "OK");
        }
        finally
        {
            // Restaurar bot�n
            if (sender is Button button)
            {
                button.Text = "Iniciar sesi�n";
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