using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class ConversationsListPage : ContentPage
{
    #region Propiedades

    private readonly ApiService _apiService;
    private readonly ObservableCollection<Conversacion> _conversaciones = new();
    private bool _isLoading = false;

    #endregion

    #region Constructor

    public ConversationsListPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    #endregion

    #region Eventos del ciclo de vida

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!SessionManager.EstaLogueado())
        {
            await DisplayAlert("Sesión requerida", "Debes iniciar sesión para ver tus conversaciones", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        await CargarConversacionesAsync();
    }

    #endregion

    #region Cargar conversaciones

    private async Task CargarConversacionesAsync()
    {
        if (_isLoading)
            return;

        try
        {
            _isLoading = true;
            LoadingFrame.IsVisible = true;
            EmptyStateView.IsVisible = false;
            RefreshButton.IsVisible = false;

            Debug.WriteLine("📋 Cargando conversaciones del usuario...");

            var request = new ReqListarConversacionesPorUsuario
            {
                SesionId = SessionManager.ObtenerSessionId()
            };

            var response = await _apiService.ListarConversacionesPorUsuarioAsync(request);

            if (response.Resultado && response.Conversaciones != null)
            {
                Debug.WriteLine($"✅ Conversaciones cargadas: {response.Conversaciones.Count}");

                _conversaciones.Clear();
                ConversationsContainer.Children.Clear();

                if (response.Conversaciones.Count == 0)
                {
                    MostrarEstadoVacio();
                }
                else
                {
                    foreach (var conversacion in response.Conversaciones.OrderByDescending(c => c.UpdatedAt))
                    {
                        _conversaciones.Add(conversacion);
                        var conversationView = CrearVistaConversacion(conversacion);
                        ConversationsContainer.Children.Add(conversationView);
                    }
                    RefreshButton.IsVisible = true;
                }
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                Debug.WriteLine($"❌ Error al cargar conversaciones: {errorMessage}");

                if (EsErrorDeSesion(errorMessage))
                {
                    await MostrarErrorSesionYRedirigir();
                    return;
                }

                await DisplayAlert("Error", $"No se pudieron cargar las conversaciones: {errorMessage}", "OK");
                MostrarEstadoVacio();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💥 Error al cargar conversaciones: {ex.Message}");
            await DisplayAlert("Error", "Error de conexión al cargar conversaciones", "OK");
            MostrarEstadoVacio();
        }
        finally
        {
            _isLoading = false;
            LoadingFrame.IsVisible = false;
        }
    }

    private void MostrarEstadoVacio()
    {
        EmptyStateView.IsVisible = true;
        RefreshButton.IsVisible = false;
    }

    #endregion

    #region Crear vista de conversación

    private View CrearVistaConversacion(Conversacion conversacion)
    {
        try
        {
            var usuarioActualId = SessionManager.ObtenerIdUsuario();
            var otroUsuario = conversacion.Usuario1.UsuarioId == usuarioActualId
                ? conversacion.Usuario2
                : conversacion.Usuario1;

            var frame = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 15,
                HasShadow = true,
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await AbrirChat(conversacion);
            frame.GestureRecognizers.Add(tapGesture);

            var mainGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = 60 },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Avatar del usuario
            var avatarFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#4A7C59"),
                CornerRadius = 25,
                WidthRequest = 50,
                HeightRequest = 50,
                HasShadow = false,
                Padding = 0,
                VerticalOptions = LayoutOptions.Center
            };

            var avatarLabel = new Label
            {
                Text = $"{otroUsuario.Nombre?.FirstOrDefault()}{otroUsuario.Apellido1?.FirstOrDefault()}",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            avatarFrame.Content = avatarLabel;

            // Información del usuario y servicio
            var nombreLabel = new Label
            {
                Text = $"{otroUsuario.Nombre} {otroUsuario.Apellido1}",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.Black,
                VerticalOptions = LayoutOptions.Center
            };

            var servicioLabel = new Label
            {
                Text = $"Servicio: {conversacion.Servicio?.Titulo ?? "N/A"}",
                FontSize = 14,
                TextColor = Colors.Gray,
                VerticalOptions = LayoutOptions.Center
            };

            // Fecha de última actividad
            var fechaLabel = new Label
            {
                Text = FormatearFecha(conversacion.UpdatedAt),
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Center
            };

            // Indicador de estado
            var estadoFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#28A745"),
                CornerRadius = 8,
                HasShadow = false,
                Padding = new Thickness(8, 4),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End
            };

            var estadoLabel = new Label
            {
                Text = "Activo",
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            estadoFrame.Content = estadoLabel;

            // Ensamblar la vista
            Grid.SetColumn(avatarFrame, 0);
            Grid.SetRowSpan(avatarFrame, 2);
            mainGrid.Children.Add(avatarFrame);

            Grid.SetColumn(nombreLabel, 1);
            Grid.SetRow(nombreLabel, 0);
            mainGrid.Children.Add(nombreLabel);

            Grid.SetColumn(servicioLabel, 1);
            Grid.SetRow(servicioLabel, 1);
            mainGrid.Children.Add(servicioLabel);

            Grid.SetColumn(fechaLabel, 2);
            Grid.SetRow(fechaLabel, 0);
            mainGrid.Children.Add(fechaLabel);

            Grid.SetColumn(estadoFrame, 2);
            Grid.SetRow(estadoFrame, 1);
            mainGrid.Children.Add(estadoFrame);

            frame.Content = mainGrid;

            return frame;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al crear vista de conversación: {ex.Message}");
            return new Label { Text = "Error al cargar conversación", TextColor = Colors.Red };
        }
    }

    #endregion

    #region Navegación y eventos

    private async Task AbrirChat(Conversacion conversacion)
    {
        try
        {
            Debug.WriteLine($"🚀 Abriendo chat - Conversación ID: {conversacion.ConversacionId}");

            var chatPage = new ChatPage(conversacion);
            await Navigation.PushAsync(chatPage);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al abrir chat: {ex.Message}");
            await DisplayAlert("Error", "No se pudo abrir el chat", "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await CargarConversacionesAsync();
    }

    private async void OnExplorarServiciosClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//main");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al navegar a servicios: {ex.Message}");
        }
    }

    #endregion

    #region Utilidades

    private static string FormatearFecha(DateTime fecha)
    {
        var ahora = DateTime.Now;
        var diferencia = ahora - fecha;

        if (diferencia.TotalMinutes < 1)
            return "Ahora";

        if (diferencia.TotalMinutes < 60)
            return $"{(int)diferencia.TotalMinutes}min";

        if (diferencia.TotalHours < 24)
            return $"{(int)diferencia.TotalHours}h";

        if (diferencia.TotalDays < 7)
            return $"{(int)diferencia.TotalDays}d";

        return fecha.ToString("dd/MM/yy");
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

    private async Task MostrarErrorSesionYRedirigir()
    {
        try
        {
            SessionManager.CerrarSesion();
            await DisplayAlert("Sesión Expirada", "Tu sesión ha expirado. Por favor, inicia sesión nuevamente.", "OK");
            await Shell.Current.GoToAsync("//login");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error al redirigir: {ex.Message}");
        }
    }

    #endregion
}