// FrontMoviles/VerificationPage.xaml.cs
using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class VerificationPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly string _userEmail;
    private bool _canResend = true;
    private int _timerSeconds = 60;

    public VerificationPage(string userEmail)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _userEmail = userEmail?.Trim().ToLower() ?? string.Empty;

        // Configurar la página
        ConfigurarPagina();
    }

    #region Configuración inicial

    private void ConfigurarPagina()
    {
        try
        {
            // Mostrar el email del usuario
            UserEmailLabel.Text = _userEmail;

            // Configurar focus en el campo de código
            VerificationCodeEntry.Focus();

            System.Diagnostics.Debug.WriteLine($"📧 Página de verificación configurada para: {_userEmail}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error configurando página: {ex.Message}");
            DisplayAlert("Error", "Error al configurar la página de verificación", "OK");
        }
    }

    #endregion

    #region Eventos de UI

    private void OnCodeTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var code = e.NewTextValue?.Trim() ?? string.Empty;

            // Habilitar/deshabilitar botón según longitud del código
            VerifyButton.IsEnabled = code.Length == 6;

            // Ocultar mensaje de error al escribir
            ErrorLabel.IsVisible = false;

            // Auto-verificar si el código tiene 6 dígitos
            if (code.Length == 6 && code.All(char.IsDigit))
            {
                OnVerifyClicked(sender, e);
            }

            System.Diagnostics.Debug.WriteLine($"🔢 Código ingresado: {code} (longitud: {code.Length})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error en cambio de texto: {ex.Message}");
        }
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(VerificationCodeEntry.Text) || VerificationCodeEntry.Text.Length != 6)
        {
            MostrarError("Por favor ingresa un código de 6 dígitos");
            return;
        }

        try
        {
            // Mostrar indicador de carga
            VerifyButton.Text = "Verificando...";
            VerifyButton.IsEnabled = false;
            ErrorLabel.IsVisible = false;

            // Crear request de verificación
            var request = new ReqVerificacion
            {
                Correo = _userEmail,
                Verificacion = int.Parse(VerificationCodeEntry.Text)
            };

            System.Diagnostics.Debug.WriteLine($"🔄 Verificando código {VerificationCodeEntry.Text} para {_userEmail}");

            // Llamar al API
            var response = await _apiService.VerificarUsuarioAsync(request);

            if (response.Resultado)
            {
                // Verificación exitosa
                System.Diagnostics.Debug.WriteLine("✅ Verificación exitosa");

                await DisplayAlert("¡Éxito!",
                    "Tu cuenta ha sido verificada correctamente. Ahora puedes iniciar sesión.",
                    "Continuar");

                // Navegar al login
                await NavigateToLogin();
            }
            else
            {
                // Verificación fallida
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Código incorrecto";
                System.Diagnostics.Debug.WriteLine($"❌ Verificación fallida: {errorMessage}");

                MostrarError("Código incorrecto. Intenta de nuevo.");

                // Limpiar el campo para nuevo intento
                VerificationCodeEntry.Text = "";
                VerificationCodeEntry.Focus();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"💥 Excepción en verificación: {ex.Message}");
            MostrarError("Error inesperado. Intenta nuevamente.");
        }
        finally
        {
            // Restaurar botón
            VerifyButton.Text = "Verificar";
            VerifyButton.IsEnabled = !string.IsNullOrWhiteSpace(VerificationCodeEntry.Text) &&
                                      VerificationCodeEntry.Text.Length == 6;
        }
    }

    private async void OnResendCodeTapped(object sender, EventArgs e)
    {
        if (!_canResend)
        {
            await DisplayAlert("Espera", $"Puedes reenviar el código en {_timerSeconds} segundos", "OK");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"📤 Reenviando código a {_userEmail}");

            // Aquí llamarías al endpoint para reenviar código
            // Por ahora, simular el reenvío
            await DisplayAlert("Código Reenviado",
                "Se ha enviado un nuevo código de verificación a tu correo electrónico.",
                "OK");

            // Iniciar timer de reenvío
            IniciarTimerReenvio();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error reenviando código: {ex.Message}");
            await DisplayAlert("Error", "No se pudo reenviar el código. Intenta nuevamente.", "OK");
        }
    }

    private async void OnChangeEmailTapped(object sender, EventArgs e)
    {
        try
        {
            string newEmail = await DisplayPromptAsync(
                "Cambiar Email",
                "Ingresa tu nuevo correo electrónico:",
                placeholder: "correo@ejemplo.com",
                keyboard: Keyboard.Email);

            if (!string.IsNullOrWhiteSpace(newEmail))
            {
                // Validar formato de email
                if (IsValidEmail(newEmail.Trim()))
                {
                    // Navegar a nueva página de verificación con el nuevo email
                    await Navigation.PushAsync(new VerificationPage(newEmail.Trim().ToLower()));
                    Navigation.RemovePage(this);
                }
                else
                {
                    await DisplayAlert("Email Inválido", "Por favor ingresa un email válido", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error cambiando email: {ex.Message}");
            await DisplayAlert("Error", "Error al cambiar email", "OK");
        }
    }

    #endregion

    #region Métodos auxiliares

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
        System.Diagnostics.Debug.WriteLine($"❌ Error mostrado: {mensaje}");
    }

    private void IniciarTimerReenvio()
    {
        _canResend = false;
        _timerSeconds = 60;

        ResendLabel.IsVisible = false;
        TimerLabel.IsVisible = true;
        TimerLabel.Text = $"Puedes reenviar en {_timerSeconds} segundos";

        // Timer para countdown
        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            _timerSeconds--;

            if (_timerSeconds > 0)
            {
                TimerLabel.Text = $"Puedes reenviar en {_timerSeconds} segundos";
                return true; // Continuar timer
            }
            else
            {
                // Timer terminado
                _canResend = true;
                TimerLabel.IsVisible = false;
                ResendLabel.IsVisible = true;
                return false; // Detener timer
            }
        });
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task NavigateToLogin()
    {
        try
        {
            // Navegar al login y limpiar el stack de navegación
            await Navigation.PushAsync(new LoginPage());

            // Remover páginas anteriores del stack
            var pagesToRemove = Navigation.NavigationStack.Take(Navigation.NavigationStack.Count - 1).ToList();
            foreach (var page in pagesToRemove)
            {
                Navigation.RemovePage(page);
            }

            System.Diagnostics.Debug.WriteLine("🚀 Navegación al login completada");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error navegando al login: {ex.Message}");

            // Fallback: reemplazar página principal
            Application.Current.MainPage = new AppShell();
        }
    }

    #endregion

    #region Lifecycle

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Enfocar en el campo de entrada al aparecer
        VerificationCodeEntry.Focus();

        System.Diagnostics.Debug.WriteLine($"👁️ VerificationPage apareció para {_userEmail}");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cleanup
        _apiService?.Dispose();

        System.Diagnostics.Debug.WriteLine("👋 VerificationPage desapareció");
    }

    #endregion
}