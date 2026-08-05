using SeguimientoFacturacion.Application.DTOs.Seguridad;
using SeguimientoFacturacion.Application.Interfaces.Security;

namespace SeguimientoFacturacion.Services.Seguridad;

/// <summary>
/// Captura localmente los datos del administrador inicial sin recibir
/// la contraseña como argumento de proceso ni almacenarla en configuración.
/// </summary>
public static class ComandoInicializacionAdministrador
{
    public const string Argumento = "--inicializar-administrador";

    /// <summary>
    /// Ejecuta la inicialización interactiva y devuelve un código de salida.
    /// </summary>
    public static async Task<int> EjecutarAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine(
                "La inicialización requiere una consola interactiva.");
            return 2;
        }

        using var scope = serviceProvider.CreateScope();

        var servicio = scope.ServiceProvider
            .GetRequiredService<
                IServicioInicializacionAdministrador>();

        if (await servicio.EstaInicializadoAsync(cancellationToken))
        {
            Console.Error.WriteLine(
                "El almacén de usuarios ya fue inicializado. " +
                "No se creó ni reemplazó ningún usuario.");
            return 3;
        }

        var contrasena = Array.Empty<char>();
        var confirmacion = Array.Empty<char>();

        try
        {
            Console.WriteLine("Inicialización segura del administrador");
            Console.WriteLine(
                "La contraseña no se mostrará ni se guardará en configuración.");

            var nombreUsuario = LeerTextoObligatorio(
                "Nombre de usuario: ");
            var nombreCompleto = LeerTextoObligatorio(
                "Nombre completo: ");

            contrasena = LeerSecreto(
                "Contraseña: ");
            confirmacion = LeerSecreto(
                "Confirmar contraseña: ");

            if (!contrasena.AsSpan().SequenceEqual(confirmacion))
            {
                Console.Error.WriteLine(
                    "Las contraseñas no coinciden. No se creó el usuario.");
                return 4;
            }

            var resultado = await servicio.InicializarAsync(
                new SolicitudInicializacionAdministradorDto
                {
                    NombreUsuario = nombreUsuario,
                    NombreCompleto = nombreCompleto,
                    Contrasena = new string(contrasena)
                },
                cancellationToken);

            if (!resultado.Creado)
            {
                Console.Error.WriteLine(
                    "Otro proceso inicializó el almacén. " +
                    "No se creó ni reemplazó ningún usuario.");
                return 3;
            }

            Console.WriteLine();
            Console.WriteLine(
                "Administrador inicial creado correctamente.");
            Console.WriteLine(
                $"Usuario: {resultado.NombreUsuario}");
            Console.WriteLine(
                $"Identificador: {resultado.UsuarioId}");
            Console.WriteLine(
                "usuarios.dat quedó cifrado mediante AES-256-GCM.");

            return 0;
        }
        catch (ArgumentException excepcion)
        {
            Console.Error.WriteLine(
                $"Datos no válidos: {excepcion.Message}");
            return 5;
        }
        finally
        {
            Array.Fill(contrasena, '\0');
            Array.Fill(confirmacion, '\0');
        }
    }

    private static string LeerTextoObligatorio(string mensaje)
    {
        Console.Write(mensaje);

        var valor = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor solicitado es obligatorio.");
        }

        return valor.Trim();
    }

    private static char[] LeerSecreto(string mensaje)
    {
        Console.Write(mensaje);
        var caracteres = new List<char>();

        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);

            if (tecla.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                var resultado = caracteres.ToArray();

                for (var indice = 0;
                    indice < caracteres.Count;
                    indice++)
                {
                    caracteres[indice] = '\0';
                }

                caracteres.Clear();
                return resultado;
            }

            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (caracteres.Count != 0)
                {
                    caracteres.RemoveAt(caracteres.Count - 1);
                }

                continue;
            }

            if (!char.IsControl(tecla.KeyChar))
            {
                caracteres.Add(tecla.KeyChar);
            }
        }
    }
}
