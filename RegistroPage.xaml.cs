using System.Net.Mail;
using System.Globalization;

namespace RegistroEstudianteMAUI;

public partial class RegistroPage : ContentPage
{
    private readonly DateTime _fechaInicial = DateTime.Today.AddYears(-18);
    private readonly List<(Label Label, Entry? Entry)> _errorFields = [];

    public RegistroPage()
    {
        InitializeComponent();
        var app = Application.Current;
        TemaSwitch.IsToggled = app?.UserAppTheme == AppTheme.Dark ||
            (app?.UserAppTheme == AppTheme.Unspecified && app?.RequestedTheme == AppTheme.Dark);
        CarreraPicker.ItemsSource = new List<string> { "Administración de Empresas", "Derecho", "Ingeniería en Sistemas", "Medicina", "Psicología" };
        CarreraPicker.SelectedIndex = -1;
        NacimientoPicker.MaximumDate = DateTime.Today;
        NacimientoPicker.Date = _fechaInicial;
        _errorFields.AddRange([(NombreError, NombreEntry), (CorreoError, CorreoEntry), (TelefonoError, TelefonoEntry), (CarreraError, null), (NacimientoError, null), (GeneroError, null), (TerminosError, null)]);
        NombreEntry.TextChanged += (_, _) => ValidarCampo("nombre");
        CorreoEntry.TextChanged += (_, _) => ValidarCampo("correo");
        TelefonoEntry.TextChanged += (_, _) => ValidarCampo("telefono");
        CarreraPicker.SelectedIndexChanged += (_, _) => ValidarCampo("carrera");
        NacimientoPicker.DateSelected += (_, _) => ValidarCampo("fecha");
        GeneroFemenino.CheckedChanged += (_, _) => ValidarCampo("genero");
        GeneroMasculino.CheckedChanged += (_, _) => ValidarCampo("genero");
        GeneroOtro.CheckedChanged += (_, _) => ValidarCampo("genero");
        TerminosCheckBox.CheckedChanged += (_, _) => ValidarCampo("terminos");
    }

    private void OnTemaToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is not null)
            Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        NombreEntry.Text = NombreEntry.Text?.Trim();
        var errores = ValidarFormulario();
        if (errores.Count > 0)
        {
            EstadoValidacionLabel.Text = $"Revisa los {errores.Count} campos indicados antes de continuar.";
            EstadoValidacionLabel.IsVisible = true;
            var primerError = _errorFields.FirstOrDefault(item => item.Label.IsVisible);
            primerError.Entry?.Focus();
            return;
        }

        EstadoValidacionLabel.IsVisible = false;
        var genero = ObtenerGenero();
        var resumen = $"El formulario se validó correctamente.\n\nNombre: {NombreEntry.Text?.Trim()}\nCorreo: {CorreoEntry.Text?.Trim()}\nTeléfono: {TelefonoEntry.Text?.Trim()}\nCarrera: {CarreraPicker.SelectedItem}\nFecha de nacimiento: {NacimientoPicker.Date:dd/MM/yyyy}\nGénero: {genero}\nTérminos aceptados: Sí";
        await DisplayAlertAsync("Registro validado", resumen, "Aceptar");
    }

    private List<string> ValidarFormulario()
    {
        var errores = new List<string>();
        errores.AddRange(ValidarCampo("nombre"));
        errores.AddRange(ValidarCampo("correo"));
        errores.AddRange(ValidarCampo("telefono"));
        errores.AddRange(ValidarCampo("carrera"));
        errores.AddRange(ValidarCampo("fecha"));
        errores.AddRange(ValidarCampo("genero"));
        errores.AddRange(ValidarCampo("terminos"));
        return errores;
    }

    private List<string> ValidarCampo(string campo)
    {
        var error = campo switch
        {
            "nombre" when string.IsNullOrWhiteSpace(NombreEntry.Text?.Trim()) => (NombreError, "Escribe tu nombre completo."),
            "nombre" when !NombreValido(NombreEntry.Text!.Trim()) => (NombreError, "Usa de 2 a 80 caracteres con letras y espacios; se permiten guiones y apóstrofes, pero no números."),
            "correo" when string.IsNullOrWhiteSpace(CorreoEntry.Text) => (CorreoError, "El correo electrónico es obligatorio."),
            "correo" when !CorreoValido(CorreoEntry.Text!) => (CorreoError, "Introduce un correo válido, por ejemplo nombre@universidad.edu."),
            "telefono" when string.IsNullOrWhiteSpace(TelefonoEntry.Text) => (TelefonoError, "El teléfono es obligatorio."),
            "telefono" when !TelefonoValido(TelefonoEntry.Text!) => (TelefonoError, "Introduce entre 7 y 15 dígitos; puedes usar +, espacios o guiones."),
            "carrera" when !CarreraSeleccionadaValida() => (CarreraError, "Selecciona una carrera de la lista."),
            "fecha" when NacimientoPicker.Date > DateTime.Today => (NacimientoError, "La fecha de nacimiento no puede ser futura."),
            "genero" when string.IsNullOrWhiteSpace(ObtenerGenero()) => (GeneroError, "Selecciona una opción de género."),
            "terminos" when !TerminosCheckBox.IsChecked => (TerminosError, "Debes aceptar los términos para continuar."),
            _ => (null, null)
        };

        if (error.Item1 is null)
        {
            var target = _errorFields.FirstOrDefault(item => item.Label == ObtenerEtiqueta(campo)).Label;
            if (target is not null) { target.Text = string.Empty; target.IsVisible = false; }
            return [];
        }

        error.Item1.Text = error.Item2;
        error.Item1.IsVisible = true;
        return [error.Item2!];
    }

    private Label? ObtenerEtiqueta(string campo) => campo switch
    {
        "nombre" => NombreError, "correo" => CorreoError, "telefono" => TelefonoError,
        "carrera" => CarreraError, "fecha" => NacimientoError, "genero" => GeneroError,
        "terminos" => TerminosError, _ => null
    };

    private static bool CorreoValido(string correo)
    {
        try { return new MailAddress(correo.Trim()).Address == correo.Trim() && correo.Contains('.'); }
        catch (FormatException) { return false; }
    }

    private static bool NombreValido(string nombre)
    {
        var limpio = nombre.Trim();
        if (limpio.Length is < 2 or > 80)
            return false;

        var contieneLetras = false;
        foreach (var caracter in limpio)
        {
            var categoria = char.GetUnicodeCategory(caracter);
            if (char.IsLetter(caracter))
            {
                contieneLetras = true;
                continue;
            }

            if (categoria is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
                continue;

            if (caracter is ' ' or '-' or '\'' or '’')
                continue;

            return false;
        }

        return contieneLetras;
    }

    private bool CarreraSeleccionadaValida() =>
        CarreraPicker.SelectedIndex >= 0 &&
        CarreraPicker.SelectedItem is string carrera &&
        CarreraPicker.ItemsSource is IEnumerable<string> carreras &&
        carreras.Contains(carrera);

    private static bool TelefonoValido(string telefono)
    {
        var digitos = telefono.Count(char.IsDigit);
        return digitos is >= 7 and <= 15 && telefono.All(c => char.IsDigit(c) || c is '+' or ' ' or '-' or '(' or ')' or '.');
    }

    private string ObtenerGenero() => GeneroFemenino.IsChecked ? "Femenino" : GeneroMasculino.IsChecked ? "Masculino" : GeneroOtro.IsChecked ? "Otro" : string.Empty;

    private void OnLimpiarClicked(object? sender, EventArgs e)
    {
        NombreEntry.Text = string.Empty;
        CorreoEntry.Text = string.Empty;
        TelefonoEntry.Text = string.Empty;
        CarreraPicker.SelectedIndex = -1;
        NacimientoPicker.Date = _fechaInicial;
        GeneroFemenino.IsChecked = GeneroMasculino.IsChecked = GeneroOtro.IsChecked = false;
        TerminosCheckBox.IsChecked = false;
        foreach (var (label, _) in _errorFields) { label.Text = string.Empty; label.IsVisible = false; }
        EstadoValidacionLabel.Text = string.Empty;
        EstadoValidacionLabel.IsVisible = false;
        NombreEntry.Focus();
    }
}
