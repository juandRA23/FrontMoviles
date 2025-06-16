using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using FrontMoviles.Servicios;
using FrontMoviles.Modelos;

namespace FrontMoviles;

public partial class ChatPage : ContentPage
{
    #region Propiedades

    private readonly ApiService _apiService;
    private readonly Conversacion _conversacion;
    private readonly Usuario _otroUsuario;
    private readonly Timer _refreshTimer;
    private bool _isFirstLoad = true;
    private readonly ObservableCollection<Mensaje> _mensajes = new();

    #endregion

    #region Obtener datos de usuario

    private async Task ObtenerDatosUsuarioDesdeAPI()
    {
        try
        {
            Debug.WriteLine("🔄 Obteniendo datos del usuario desde la API...");

            var response = await _apiService.ObtenerPerfilUsuarioAsync();

            if (response.Resultado && response.Usuario != null)
            {
                var usuario = response.Usuario;
                Debug.WriteLine($"✅ Usuario obtenido desde API - ID: {usuario.UsuarioId}, Nombre: {usuario.Nombre}");

                // Usar los datos obtenidos de la API para enviar el mensaje
                await EnviarMensajeConUsuario(usuario);
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                Debug.WriteLine($"❌ Error obteniendo usuario desde API: {errorMessage}");
                await DisplayAlert("Error", "No se pudo obtener información del usuario. Intenta cerrar sesión e iniciar sesión nuevamente.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💥 Error obteniendo usuario desde API: {ex.Message}");
            await DisplayAlert("Error", "Error al obtener información del usuario", "OK");
        }
    }

    private async Task EnviarMensajeConUsuario(Usuario usuario)
    {
        try
        {
            var textoMensaje = MessageEntry.Text?.Trim();

            if (string.IsNullOrEmpty(textoMensaje))
            {
                await DisplayAlert("Mensaje vacío", "Por favor escribe un mensaje", "OK");
                return;
            }

            // Deshabilitar botón mientras se envía
            SendButton.IsEnabled = false;
            SendButton.Text = "Enviando...";

            var mensaje = new Mensaje
            {
                MensajeId = 0, // Se asigna en el servidor
                Conversacion = _conversacion,
                Usuario = usuario,
                Contenido = textoMensaje,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var request = new ReqInsertarMensaje
            {
                SesionId = SessionManager.ObtenerSessionId(),
                Mensaje = mensaje
            };

            Debug.WriteLine($"📤 Enviando mensaje con usuario desde API: {textoMensaje}");
            Debug.WriteLine($"👤 Usuario: {usuario.Nombre} (ID: {usuario.UsuarioId})");

            var response = await _apiService.InsertarMensajeAsync(request);

            if (response.Resultado)
            {
                Debug.WriteLine("✅ Mensaje enviado exitosamente con usuario desde API");

                // Limpiar campo de texto
                MessageEntry.Text = string.Empty;

                // Cargar mensajes actualizados inmediatamente
                await CargarMensajesAsync();
                ScrollToBottom();
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                Debug.WriteLine($"❌ Error al enviar mensaje: {errorMessage}");
                await DisplayAlert("Error", $"No se pudo enviar el mensaje: {errorMessage}", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💥 Error enviando mensaje con usuario desde API: {ex.Message}");
            await DisplayAlert("Error", "Error al enviar mensaje", "OK");
        }
        finally
        {
            // Restaurar botón
            SendButton.IsEnabled = true;
            SendButton.Text = "Enviar";
        }
    }

    #endregion

    #region Mensaje de bienvenida

    private void MostrarMensajeBienvenida()
    {
        try
        {
            var welcomeFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#E3F2FD"),
                CornerRadius = 15,
                HasShadow = false,
                Padding = new Thickness(20, 15),
                Margin = new Thickness(20, 10),
                HorizontalOptions = LayoutOptions.Center
            };

            var welcomeStack = new StackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Center
            };

            welcomeStack.Children.Add(new Label
            {
                Text = "💬",
                FontSize = 30,
                HorizontalOptions = LayoutOptions.Center
            });

            welcomeStack.Children.Add(new Label
            {
                Text = "¡Conversación iniciada!",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1976D2"),
                HorizontalOptions = LayoutOptions.Center
            });

            welcomeStack.Children.Add(new Label
            {
                Text = $"Ahora puedes chatear con {_otroUsuario.Nombre} sobre el servicio \"{_conversacion.Servicio?.Titulo}\"",
                FontSize = 14,
                TextColor = Color.FromArgb("#424242"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            });

            welcomeFrame.Content = welcomeStack;
            MessagesContainer.Children.Add(welcomeFrame);

            Debug.WriteLine("✅ Mensaje de bienvenida mostrado");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error mostrando mensaje de bienvenida: {ex.Message}");
        }
    }

    #endregion

    #region Constructor

    public ChatPage(Conversacion conversacion)
    {
        InitializeComponent();
        _apiService = new ApiService();
        _conversacion = conversacion;

        // Determinar quién es el otro usuario
        var usuarioActualId = SessionManager.ObtenerIdUsuario();
        _otroUsuario = conversacion.Usuario1.UsuarioId == usuarioActualId
            ? conversacion.Usuario2
            : conversacion.Usuario1;

        ConfigurarPagina();

        // Timer para actualizar mensajes cada 3 segundos
        _refreshTimer = new Timer(async _ => await CargarMensajesAsync(), null,
            TimeSpan.Zero, TimeSpan.FromSeconds(3));
    }

    #endregion

    #region Configuración inicial

    private void ConfigurarPagina()
    {
        Title = $"Chat con {_otroUsuario.Nombre} {_otroUsuario.Apellido1}";
        Debug.WriteLine($"💬 Iniciando chat - Conversación ID: {_conversacion.ConversacionId}");
        Debug.WriteLine($"💬 Otro usuario: {_otroUsuario.Nombre} {_otroUsuario.Apellido1}");
    }

    #endregion

    #region Cargar mensajes

    private async Task CargarMensajesAsync()
    {
        try
        {
            if (_isFirstLoad)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoadingFrame.IsVisible = true;
                });
            }

            Debug.WriteLine($"📡 === CARGANDO MENSAJES ===");
            Debug.WriteLine($"🏠 Conversación ID: {_conversacion.ConversacionId}");

            var request = new ReqListarMensajesPorConversacion
            {
                Conversacion = _conversacion
            };

            var response = await _apiService.ListarMensajesPorConversacionAsync(request);

            Debug.WriteLine($"📥 Respuesta recibida - Resultado: {response.Resultado}");
            Debug.WriteLine($"📥 Mensajes en respuesta: {response.Mensajes?.Count ?? 0}");

            if (response.Resultado && response.Mensajes != null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ActualizarMensajes(response.Mensajes);

                    if (_isFirstLoad)
                    {
                        LoadingFrame.IsVisible = false;
                        _isFirstLoad = false;
                        ScrollToBottom();
                    }
                });
            }
            else
            {
                // Para conversaciones nuevas sin mensajes, esto es normal
                if (_isFirstLoad)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        LoadingFrame.IsVisible = false;
                        _isFirstLoad = false;

                        // Mostrar mensaje de bienvenida para conversación nueva
                        MostrarMensajeBienvenida();
                    });
                }

                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                Debug.WriteLine($"❌ Info de mensajes: {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💥 Error al cargar mensajes: {ex.Message}");

            if (_isFirstLoad)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    LoadingFrame.IsVisible = false;
                    await DisplayAlert("Error", "Error de conexión al cargar mensajes", "OK");
                });
            }
        }
    }

    private void ActualizarMensajes(List<Mensaje> mensajesNuevos)
    {
        try
        {
            Debug.WriteLine($"🔄 === ACTUALIZANDO MENSAJES ===");
            Debug.WriteLine($"📥 Mensajes recibidos: {mensajesNuevos?.Count ?? 0}");
            Debug.WriteLine($"📋 Mensajes actuales en UI: {_mensajes.Count}");

            if (mensajesNuevos == null || mensajesNuevos.Count == 0)
            {
                Debug.WriteLine("⚠️ No hay mensajes nuevos para mostrar");
                return;
            }

            // Ordenar mensajes por fecha
            var mensajesOrdenados = mensajesNuevos
                .OrderBy(m => m.CreatedAt)
                .ToList();

            Debug.WriteLine($"📊 Mensajes ordenados: {mensajesOrdenados.Count}");

            // Imprimir cada mensaje para debug
            for (int i = 0; i < mensajesOrdenados.Count; i++)
            {
                var msg = mensajesOrdenados[i];
                Debug.WriteLine($"  [{i}] ID: {msg.MensajeId}, Usuario: {msg.Usuario?.Nombre}, Contenido: '{msg.Contenido}', Fecha: {msg.CreatedAt:HH:mm:ss}");
            }

            // Si no hay cambios, no actualizar la UI
            if (_mensajes.Count == mensajesOrdenados.Count)
            {
                var ultimoMensaje = _mensajes.LastOrDefault();
                var ultimoMensajeNuevo = mensajesOrdenados.LastOrDefault();

                if (ultimoMensaje?.MensajeId == ultimoMensajeNuevo?.MensajeId)
                {
                    Debug.WriteLine("🔄 Los mensajes son iguales, no hay cambios");
                    return;
                }
            }

            Debug.WriteLine("🧹 Limpiando UI y agregando mensajes actualizados...");

            // Limpiar y agregar mensajes actualizados
            MessagesContainer.Children.Clear();
            _mensajes.Clear();

            foreach (var mensaje in mensajesOrdenados)
            {
                _mensajes.Add(mensaje);
                var messageView = CrearVistaMensaje(mensaje);
                MessagesContainer.Children.Add(messageView);
                Debug.WriteLine($"➕ Agregado mensaje: '{mensaje.Contenido.Substring(0, Math.Min(mensaje.Contenido.Length, 20))}...'");
            }

            Debug.WriteLine($"✅ UI actualizada con {_mensajes.Count} mensajes");
            Debug.WriteLine("================================");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💥 Error al actualizar mensajes: {ex.Message}");
            Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");
        }
    }

    #endregion

    #region Crear vista de mensaje

    private View CrearVistaMensaje(Mensaje mensaje)
    {
        var usuarioActualId = SessionManager.ObtenerIdUsuario();
        var esMensajePropio = mensaje.Usuario.UsuarioId == usuarioActualId;

        var frame = new Frame
        {
            CornerRadius = 15,
            HasShadow = false,
            Padding = new Thickness(15, 10),
            Margin = new Thickness(esMensajePropio ? 50 : 0, 5, esMensajePropio ? 0 : 50, 5),
            BackgroundColor = esMensajePropio ? Color.FromArgb("#4A7C59") : Color.FromArgb("#E9ECEF"),
            HorizontalOptions = esMensajePropio ? LayoutOptions.End : LayoutOptions.Start
        };

        var stackLayout = new StackLayout
        {
            Spacing = 5
        };

        // Contenido del mensaje
        var contentLabel = new Label
        {
            Text = mensaje.Contenido,
            TextColor = esMensajePropio ? Colors.White : Colors.Black,
            FontSize = 16,
            LineBreakMode = LineBreakMode.WordWrap
        };

        // Información de fecha y usuario
        var infoText = esMensajePropio
            ? $"Tú • {mensaje.CreatedAt:HH:mm}"
            : $"{mensaje.Usuario.Nombre} • {mensaje.CreatedAt:HH:mm}";

        var infoLabel = new Label
        {
            Text = infoText,
            TextColor = esMensajePropio ? Color.FromArgb("#B8D4C1") : Colors.Gray,
            FontSize = 12,
            HorizontalOptions = esMensajePropio ? LayoutOptions.End : LayoutOptions.Start
        };

        stackLayout.Children.Add(contentLabel);
        stackLayout.Children.Add(infoLabel);
        frame.Content = stackLayout;

        return frame;
    }

    #endregion

    #region Enviar mensaje

    private async void OnSendMessageClicked(object sender, EventArgs e)
    {
        await EnviarMensajeAsync();
    }

    private async Task EnviarMensajeAsync()
    {
        try
        {
            var textoMensaje = MessageEntry.Text?.Trim();

            if (string.IsNullOrEmpty(textoMensaje))
            {
                await DisplayAlert("Mensaje vacío", "Por favor escribe un mensaje", "OK");
                return;
            }

            // Deshabilitar botón mientras se envía
            SendButton.IsEnabled = false;
            SendButton.Text = "Enviando...";

            // Crear objeto usuario actual desde SessionManager usando métodos existentes
            var usuarioActualId = SessionManager.ObtenerIdUsuario();
            var usuarioActualEmail = SessionManager.ObtenerEmailUsuario();
            var usuarioActualNombre = SessionManager.ObtenerNombreUsuario();

            // DEBUG: Imprimir todos los valores del SessionManager
            Debug.WriteLine("🔍 === DEBUG SESSIONMANAGER ===");
            Debug.WriteLine($"ID Usuario: {usuarioActualId}");
            Debug.WriteLine($"Email Usuario: '{usuarioActualEmail}'");
            Debug.WriteLine($"Nombre Usuario: '{usuarioActualNombre}'");
            Debug.WriteLine($"SessionId: '{SessionManager.ObtenerSessionId()}'");
            Debug.WriteLine($"Está logueado: {SessionManager.EstaLogueado()}");
            Debug.WriteLine("================================");

            if (usuarioActualId == 0 || string.IsNullOrEmpty(usuarioActualEmail))
            {
                Debug.WriteLine($"❌ Datos de usuario insuficientes - ID: {usuarioActualId}, Email: '{usuarioActualEmail}'");

                // Intentar obtener el ID del usuario desde la API
                Debug.WriteLine("🔄 Intentando obtener información del usuario desde la API...");
                await ObtenerDatosUsuarioDesdeAPI();
                return;
            }

            // Si no hay nombre, usar la parte del email antes del @
            var nombreParaMostrar = !string.IsNullOrEmpty(usuarioActualNombre)
                ? usuarioActualNombre
                : usuarioActualEmail.Split('@')[0];

            var usuarioActual = new Usuario
            {
                UsuarioId = usuarioActualId,
                Correo = usuarioActualEmail,
                Nombre = nombreParaMostrar,
                Apellido1 = string.Empty,
                Apellido2 = string.Empty,
                // Agregar campos requeridos con valores por defecto
                FechaNacimiento = DateTime.Now.AddYears(-25), // Fecha por defecto
                Telefono = string.Empty,
                Direccion = string.Empty,
                Contrasena = string.Empty,
                Salt = string.Empty,
                Verificacion = 1,
                Activo = true,
                PerfilCompleto = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            Debug.WriteLine($"✅ Usuario creado para mensaje - ID: {usuarioActual.UsuarioId}, Nombre: {usuarioActual.Nombre}");

            var mensaje = new Mensaje
            {
                MensajeId = 0, // Se asigna en el servidor
                Conversacion = _conversacion,
                Usuario = usuarioActual,
                Contenido = textoMensaje,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var request = new ReqInsertarMensaje
            {
                SesionId = SessionManager.ObtenerSessionId(),
                Mensaje = mensaje
            };

            Debug.WriteLine($"📤 Enviando mensaje: {textoMensaje}");
            Debug.WriteLine($"👤 Usuario: {usuarioActual.Nombre} (ID: {usuarioActual.UsuarioId})");
            Debug.WriteLine($"🏠 Conversación: {_conversacion.ConversacionId}");

            var response = await _apiService.InsertarMensajeAsync(request);

            if (response.Resultado)
            {
                Debug.WriteLine("✅ Mensaje enviado exitosamente");
                Debug.WriteLine($"📝 Limpiando campo de texto: '{MessageEntry.Text}' → ''");

                // Limpiar campo de texto
                MessageEntry.Text = string.Empty;
                Debug.WriteLine($"📝 Campo limpiado. Nuevo valor: '{MessageEntry.Text}'");

                // Esperar un momento antes de cargar mensajes
                await Task.Delay(500);

                Debug.WriteLine("🔄 Cargando mensajes actualizados...");
                // Cargar mensajes actualizados inmediatamente
                await CargarMensajesAsync();

                Debug.WriteLine("⬇️ Haciendo scroll hacia abajo...");
                ScrollToBottom();
            }
            else
            {
                var errorMessage = response.Error?.FirstOrDefault()?.Message ?? "Error desconocido";
                Debug.WriteLine($"❌ Error al enviar mensaje: {errorMessage}");
                Debug.WriteLine($"❌ Detalles del error completo: {string.Join(", ", response.Error?.Select(e => $"[{e.ErrorCode}] {e.Message}") ?? new[] { "Sin detalles" })}");

                if (EsErrorDeSesion(errorMessage))
                {
                    await MostrarErrorSesionYRedirigir();
                    return;
                }

                // Mostrar error más descriptivo
                var mensajeError = errorMessage.Contains("usuario") || errorMessage.Contains("información")
                    ? "Error con la información del usuario. Intenta cerrar sesión e iniciar sesión nuevamente."
                    : $"No se pudo enviar el mensaje: {errorMessage}";

                await DisplayAlert("Error", mensajeError, "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"💥 Error al enviar mensaje: {ex.Message}");
            await DisplayAlert("Error", "Error de conexión al enviar mensaje", "OK");
        }
        finally
        {
            // Restaurar botón
            SendButton.IsEnabled = true;
            SendButton.Text = "Enviar";
        }
    }

    #endregion

    #region Utilidades

    private void ScrollToBottom()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await MessagesScrollView.ScrollToAsync(0, MessagesContainer.Height, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al hacer scroll: {ex.Message}");
            }
        });
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

    #region Limpieza

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer?.Dispose();
        Debug.WriteLine("💬 Chat cerrado - Timer disposed");
    }

    #endregion
}