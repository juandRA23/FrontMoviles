using FrontMoviles.Servicios;
using System.ComponentModel;

namespace FrontMoviles;

public partial class InicioPage : ContentPage
{
    public InicioPage()
    {
        InitializeComponent();
        CargarDatosIniciales();
        CargarServiciosRecientes();
    }

    #region Configuración Inicial

    private void CargarDatosIniciales()
    {
        // Personalizar saludo con el usuario logueado
        var userEmail = SessionManager.ObtenerEmailUsuario();
        if (!string.IsNullOrEmpty(userEmail))
        {
            var nombreUsuario = userEmail.Split('@')[0]; // Obtener parte antes del @
            SaludoLabel.Text = $"¡Hola {nombreUsuario}!";
        }
    }

    private void CargarServiciosRecientes()
    {
        // Por ahora cargar datos mock - después conectar con el API
        var serviciosMock = new List<ServicioMock>
        {
            new ServicioMock
            {
                Titulo = "Tutoría de Matemáticas",
                Descripcion = "Cálculo, Álgebra, Estadística",
                Precio = "₡12,500/hr",
                Calificacion = "4.9",
                Categoria = "EDUCACIÓN",
                Icono = "📊"
            },
            new ServicioMock
            {
                Titulo = "Electricista profesional",
                Descripcion = "Instalaciones y reparaciones",
                Precio = "₡17,500/hr",
                Calificacion = "4.7",
                Categoria = "HOGAR",
                Icono = "⚡"
            },
            new ServicioMock
            {
                Titulo = "Desarrollo Web",
                Descripcion = "Sitios web y aplicaciones",
                Precio = "₡25,000/hr",
                Calificacion = "4.8",
                Categoria = "TECNOLOGÍA",
                Icono = "💻"
            }
        };

        // Crear UI para cada servicio
        foreach (var servicio in serviciosMock)
        {
            var servicioFrame = CrearServicioUI(servicio);
            ServiciosContainer.Children.Add(servicioFrame);
        }
    }

    private Frame CrearServicioUI(ServicioMock servicio)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Color.FromArgb("#E0E0E0"),
            CornerRadius = 10,
            HasShadow = true,
            Padding = 15,
            Margin = new Thickness(0, 5)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(60) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(60) }
            }
        };

        // Icono del servicio
        var iconFrame = new Frame
        {
            BackgroundColor = Color.FromArgb("#A8D5BA"),
            CornerRadius = 8,
            WidthRequest = 50,
            HeightRequest = 50,
            HasShadow = false,
            Padding = 0
        };

        var iconLabel = new Label
        {
            Text = servicio.Icono,
            FontSize = 24,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        iconFrame.Content = iconLabel;
        grid.SetColumn(iconFrame, 0);
        grid.Children.Add(iconFrame);

        // Información del servicio
        var infoStack = new StackLayout
        {
            Spacing = 3,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(15, 0, 0, 0)
        };

        infoStack.Children.Add(new Label
        {
            Text = servicio.Categoria,
            FontSize = 12,
            TextColor = Color.FromArgb("#4A90A4"),
            FontAttributes = FontAttributes.Bold
        });

        infoStack.Children.Add(new Label
        {
            Text = servicio.Titulo,
            FontSize = 16,
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold
        });

        infoStack.Children.Add(new Label
        {
            Text = servicio.Descripcion,
            FontSize = 14,
            TextColor = Colors.Gray
        });

        infoStack.Children.Add(new Label
        {
            Text = servicio.Precio,
            FontSize = 14,
            TextColor = Color.FromArgb("#4A90A4"),
            FontAttributes = FontAttributes.Bold
        });

        grid.SetColumn(infoStack, 1);
        grid.Children.Add(infoStack);

        // Calificación
        var ratingStack = new StackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        var ratingLabel = new StackLayout
        {
            Orientation = StackOrientation.Horizontal,
            Spacing = 3
        };

        ratingLabel.Children.Add(new Label
        {
            Text = "★",
            FontSize = 16,
            TextColor = Color.FromArgb("#FFD700")
        });

        ratingLabel.Children.Add(new Label
        {
            Text = servicio.Calificacion,
            FontSize = 14,
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold
        });

        ratingStack.Children.Add(ratingLabel);
        grid.SetColumn(ratingStack, 2);
        grid.Children.Add(ratingStack);

        frame.Content = grid;

        // Agregar gesto de tap
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => OnServicioClicked(servicio);
        frame.GestureRecognizers.Add(tapGesture);

        return frame;
    }

    #endregion

    #region Eventos de Header

    private async void OnNotificacionesClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Notificaciones", "No tienes notificaciones nuevas", "OK");
    }

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        var textoBusqueda = BusquedaEntry.Text;
        if (!string.IsNullOrWhiteSpace(textoBusqueda))
        {
            await DisplayAlert("Búsqueda", $"Buscando: {textoBusqueda}", "OK");
            // Aquí navegarías a la página de resultados de búsqueda
        }
    }

    #endregion

    #region Eventos de Categorías

    private async void OnCategoriaClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string categoria)
        {
            await DisplayAlert("Categoría", $"Seleccionaste: {categoria}", "OK");
            // Aquí navegarías a la página de servicios por categoría
        }
    }

    #endregion

    #region Eventos de Servicios

    private async void OnServicioClicked(ServicioMock servicio)
    {
        await DisplayAlert("Servicio", $"Seleccionaste: {servicio.Titulo}", "OK");
        // Aquí navegarías a la página de detalle del servicio
    }

    private async void OnVerTodosClicked(object sender, EventArgs e)
    {
        // Cambiar a la pestaña de servicios
        CambiarPestana("Servicios");
    }

    #endregion

    #region Footer Navigation

    private void OnInicioClicked(object sender, EventArgs e)
    {
        // Ya estamos en inicio, solo actualizar UI
        CambiarPestana("Inicio");
    }

    private async void OnServiciosClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Servicios", "Navegando a lista de servicios...", "OK");
        CambiarPestana("Servicios");
        // Aquí navegarías a la página de servicios o cambiarías el contenido
    }

    private async void OnPublicarClicked(object sender, EventArgs e)
    {
       // await Navigation.PushAsync(new PublicarServicioPage());
        //CambiarPestana("Publicar");
    }

    private async void OnPerfilClicked(object sender, EventArgs e)
    {
        //await Navigation.PushAsync(new PerfilPage());
        //CambiarPestana("Perfil");
    }

    private void CambiarPestana(string pestanaActiva)
    {
        // Resetear todos los frames a color gris
        InicioFrame.BackgroundColor = Color.FromArgb("#E0E0E0");
        ServiciosFrame.BackgroundColor = Color.FromArgb("#E0E0E0");
        PublicarFrame.BackgroundColor = Color.FromArgb("#E0E0E0");
        PerfilFrame.BackgroundColor = Color.FromArgb("#E0E0E0");

        // Resetear colores de texto
        foreach (var stack in new[] { InicioFrame.Parent, ServiciosFrame.Parent, PublicarFrame.Parent, PerfilFrame.Parent })
        {
            if (stack is StackLayout stackLayout && stackLayout.Children.Count > 1)
            {
                if (stackLayout.Children[1] is Label label)
                {
                    label.TextColor = Colors.Gray;
                    label.FontAttributes = FontAttributes.None;
                }
            }
        }

        // Activar la pestaña seleccionada
        switch (pestanaActiva)
        {
            case "Inicio":
                InicioFrame.BackgroundColor = Color.FromArgb("#4A7C59");
                if (InicioFrame.Parent is StackLayout inicioStack && inicioStack.Children.Count > 1)
                {
                    if (inicioStack.Children[1] is Label inicioLabel)
                    {
                        inicioLabel.TextColor = Color.FromArgb("#4A7C59");
                        inicioLabel.FontAttributes = FontAttributes.Bold;
                    }
                }
                break;
            case "Servicios":
                ServiciosFrame.BackgroundColor = Color.FromArgb("#4A7C59");
                if (ServiciosFrame.Parent is StackLayout serviciosStack && serviciosStack.Children.Count > 1)
                {
                    if (serviciosStack.Children[1] is Label serviciosLabel)
                    {
                        serviciosLabel.TextColor = Color.FromArgb("#4A7C59");
                        serviciosLabel.FontAttributes = FontAttributes.Bold;
                    }
                }
                break;
            case "Publicar":
                PublicarFrame.BackgroundColor = Color.FromArgb("#4A7C59");
                if (PublicarFrame.Parent is StackLayout publicarStack && publicarStack.Children.Count > 1)
                {
                    if (publicarStack.Children[1] is Label publicarLabel)
                    {
                        publicarLabel.TextColor = Color.FromArgb("#4A7C59");
                        publicarLabel.FontAttributes = FontAttributes.Bold;
                    }
                }
                break;
            case "Perfil":
                PerfilFrame.BackgroundColor = Color.FromArgb("#4A7C59");
                if (PerfilFrame.Parent is StackLayout perfilStack && perfilStack.Children.Count > 1)
                {
                    if (perfilStack.Children[1] is Label perfilLabel)
                    {
                        perfilLabel.TextColor = Color.FromArgb("#4A7C59");
                        perfilLabel.FontAttributes = FontAttributes.Bold;
                    }
                }
                break;
        }
    }

    #endregion

    #region Cleanup

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    #endregion
}

#region Modelos Mock
public class ServicioMock
{
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public string Precio { get; set; }
    public string Calificacion { get; set; }
    public string Categoria { get; set; }
    public string Icono { get; set; }
}
#endregion