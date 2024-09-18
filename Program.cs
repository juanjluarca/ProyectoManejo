using Newtonsoft.Json;
using System.Text;

public class ArchivoFAT
{
    public string Nombre { get; set; }
    public string RutaInicial { get; set; }
    public bool Papelera { get; set; } = false;
    public int TamañoTotal { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }
}

public class BloqueDatos
{
    public string Datos { get; set; }
    public string RutaSiguiente { get; set; }
    public bool EOF { get; set; } = false;
}

public class FATSystem
{
    private const int MaxCaracteresPorBloque = 20;
    private string directorioBase = "ArchivosFAT/";

    public FATSystem()
    {
        // Crear el directorio base si no existe
        if (!Directory.Exists(directorioBase))
        {
            Directory.CreateDirectory(directorioBase);
        }
    }

    public void CreateFile(string nombre, string contenido)
    {
        string rutaFAT = directorioBase + nombre + ".txt";
        ArchivoFAT archivoFAT = new ArchivoFAT
        {
            Nombre = nombre,
            FechaCreacion = DateTime.Now,
            FechaModificacion = DateTime.Now,
            TamañoTotal = contenido.Length
        };

        // Se crea la lista "bloques" que almacena el contenido divido en strings de 20 caracteres
        List<BloqueDatos> bloques = DivideInBlocks(contenido);
        archivoFAT.RutaInicial = SaveBlocks(bloques, nombre);

        // Se serialza la tabla fat a un archivo json
        File.WriteAllText(rutaFAT, JsonConvert.SerializeObject(archivoFAT, Formatting.Indented));
    }

    private List<BloqueDatos> DivideInBlocks(string contenido)
    {
        List<BloqueDatos> bloques = new List<BloqueDatos>();
        for (int i = 0; i < contenido.Length; i += MaxCaracteresPorBloque)
        {
            string fragmento = contenido.Substring(i, Math.Min(MaxCaracteresPorBloque, contenido.Length - i));
            bloques.Add(new BloqueDatos { Datos = fragmento });
        }

        // Se marca el último bloque con EOF = true
        if (bloques.Count > 0)
        {
            bloques[bloques.Count - 1].EOF = true;
        }

        return bloques;
    }

    private string SaveBlocks(List<BloqueDatos> bloques, string nombre)
    {
        string rutaAnterior = null;
        for (int i = bloques.Count - 1; i >= 0; i--)
        {
            string rutaBloque = directorioBase + nombre + i + ".json";
            bloques[i].RutaSiguiente = rutaAnterior;
            File.WriteAllText(rutaBloque, JsonConvert.SerializeObject(bloques[i], Formatting.Indented));
            rutaAnterior = rutaBloque;
        }
        return rutaAnterior; // Ruta del primer bloque
    }

    public void ListFiles()
    {
        string[] archivosFAT = Directory.GetFiles(directorioBase, "*.txt");

        foreach (string archivoFAT in archivosFAT)
        {
            ArchivoFAT archivo = JsonConvert.DeserializeObject<ArchivoFAT>(File.ReadAllText(archivoFAT));
            if (!archivo.Papelera)
            {
                Console.WriteLine($"El archivo: {archivo.Nombre}\nTamaño: {archivo.TamañoTotal} caracteres - Creado: {archivo.FechaCreacion} - Modificado: {archivo.FechaModificacion}");
            }
        }
    }

    public void OpenFile(string nombreArchivo)
    {
        string rutaFAT = directorioBase + nombreArchivo + ".txt";
        if (!File.Exists(rutaFAT))
        {
            Console.WriteLine("El archivo no existe.");
            return;
        }

        ArchivoFAT archivoFAT = JsonConvert.DeserializeObject<ArchivoFAT>(File.ReadAllText(rutaFAT));
        if (archivoFAT.Papelera)
        {
            Console.WriteLine("El archivo está en la papelera de reciclaje.");
            return;
        }

        Console.WriteLine($"Archivo: {archivoFAT.Nombre}\nTamaño: {archivoFAT.TamañoTotal} caracteres\nCreado: {archivoFAT.FechaCreacion}\nModificado: {archivoFAT.FechaModificacion}");

        string contenido = ReadBlockContent(archivoFAT.RutaInicial);
        Console.WriteLine("Contenido del archivo:");
        Console.WriteLine(contenido);
    }

    private string ReadBlockContent(string rutaInicial)
    {
        string contenidoCompleto = "";
        string rutaActual = rutaInicial;

        while (rutaActual != null)
        {
            BloqueDatos bloque = JsonConvert.DeserializeObject<BloqueDatos>(File.ReadAllText(rutaActual));
            contenidoCompleto += bloque.Datos;
            rutaActual = bloque.RutaSiguiente;
        }

        return contenidoCompleto;
    }

    public void ModifyFile(string nombreArchivo, string nuevoContenido)
{
    string rutaFAT = directorioBase + nombreArchivo + ".txt";
    
    if (!File.Exists(rutaFAT))
    {
        Console.WriteLine("El archivo no existe.");
        return;
    }
    // Se deserializa el archivo fat buscado
    ArchivoFAT archivoFAT = JsonConvert.DeserializeObject<ArchivoFAT>(File.ReadAllText(rutaFAT));

    if (archivoFAT.Papelera)
    {
        Console.WriteLine("El archivo está en la papelera de reciclaje.");
        return;
    }

    // Mostrar el contenido actual
    Console.WriteLine("Contenido actual del archivo:");
    string contenidoActual = ReadBlockContent(archivoFAT.RutaInicial);
    Console.WriteLine(contenidoActual);

    // Confirmar si se desea guardar los cambios
    Console.WriteLine("\n¿Deseas guardar los cambios? (S/N)");
    string confirmacion = Console.ReadLine();

    if (confirmacion.ToLower() == "s")
    {
        // Eliminar los bloques de datos anteriores
        DeleteDataBlock(archivoFAT.RutaInicial);

        // Actualizar la tabla FAT con los nuevos datos
        archivoFAT.TamañoTotal = nuevoContenido.Length;
        archivoFAT.FechaModificacion = DateTime.Now;

        // Dividir el nuevo contenido en bloques de 20 caracteres y guardarlos
        List<BloqueDatos> bloques = DivideInBlocks(nuevoContenido);
        archivoFAT.RutaInicial = SaveBlocks(bloques, nombreArchivo);

        // Guardar los cambios en la tabla FAT
        File.WriteAllText(rutaFAT, JsonConvert.SerializeObject(archivoFAT, Formatting.Indented));

        Console.WriteLine("El archivo ha sido modificado exitosamente.");
    }
    else
    {
        Console.WriteLine("Modificación cancelada.");
    }
}

private void DeleteDataBlock(string rutaInicial)
{
    string rutaActual = rutaInicial;

    while (rutaActual != null)
    {
        BloqueDatos bloque = JsonConvert.DeserializeObject<BloqueDatos>(File.ReadAllText(rutaActual));
        File.Delete(rutaActual);
        rutaActual = bloque.RutaSiguiente;
    }
}

    public void DeleteFile(string nombreArchivo)
    {
        string rutaFAT = directorioBase + nombreArchivo + ".txt";
        if (File.Exists(rutaFAT))
        {
            ArchivoFAT archivoFAT = JsonConvert.DeserializeObject<ArchivoFAT>(File.ReadAllText(rutaFAT));
            archivoFAT.Papelera = true;
            archivoFAT.FechaEliminacion = DateTime.Now;
            File.WriteAllText(rutaFAT, JsonConvert.SerializeObject(archivoFAT, Formatting.Indented));
            Console.WriteLine("El archivo ha sido movido a la papelera de reciclaje.");
        }
        else
        {
            Console.WriteLine("El archivo no existe.");
        }
    }

    public void RecoverFile(string nombreArchivo)
    {
        string rutaFAT = directorioBase + nombreArchivo + ".txt";
        if (File.Exists(rutaFAT))
        {
            ArchivoFAT archivoFAT = JsonConvert.DeserializeObject<ArchivoFAT>(File.ReadAllText(rutaFAT));
            if (archivoFAT.Papelera)
            {
                archivoFAT.Papelera = false;
                archivoFAT.FechaEliminacion = null;
                File.WriteAllText(rutaFAT, JsonConvert.SerializeObject(archivoFAT, Formatting.Indented));
                Console.WriteLine("El archivo ha sido recuperado.");
            }
            else
            {
                Console.WriteLine("El archivo no está en la papelera.");
            }
        }
        else
        {
            Console.WriteLine("El archivo no existe.");
        }
    }
}

class Program
{
    static FATSystem FATSystem = new FATSystem();

    static void Main(string[] args)
{
    bool salir = false;

    while (!salir)
    {   
        Console.Clear();
        Console.WriteLine("\nMenú:");
        Console.WriteLine("1. Crear un archivo");
        Console.WriteLine("2. Listar archivos");
        Console.WriteLine("3. Abrir un archivo");
        Console.WriteLine("4. Modificar un archivo");
        Console.WriteLine("5. Eliminar un archivo");
        Console.WriteLine("6. Recuperar un archivo");
        Console.WriteLine("7. Salir");

        Console.Write("Seleccione una opción: ");
        string option = Console.ReadLine();

        switch (option)
        {
            case "1":
                CreateFile();
                Console.ReadKey();
                break;
            case "2":
                FATSystem.ListFiles();
                Console.ReadKey();
                break;
            case "3":
                FATSystem.ListFiles();
                OpenFile();
                Console.ReadKey();
                break;
            case "4":
                ModifyFile();
                Console.ReadKey();
                break;
            case "5":
                DeleteFile();
                Console.ReadKey();
                break;
            case "6":
                RecoverFile();
                Console.ReadKey();
                break;
            case "7":
                salir = true;
                break;
            default:
                Console.WriteLine("Opción no válida");
                Console.ReadKey();
                break;
        }
    }
}

static void ModifyFile()
{
    Console.Write("Ingrese el nombre del archivo a modificar: ");
    string nombre = Console.ReadLine();

    Console.WriteLine("Ingrese el texto (Presione ESCAPE para terminar):");

    StringBuilder texto = new StringBuilder();
    bool terminado = false;

    while (!terminado)
    {
        ConsoleKeyInfo tecla = Console.ReadKey(intercept: true); // Captura la tecla sin mostrarla inmediatamente

        if (tecla.Key == ConsoleKey.Escape)  // Si se presiona ESCAPE, se termina la entrada
        {
            terminado = true;
        }
        else if (tecla.Key == ConsoleKey.Enter)  // Si se presiona ENTER, se agrega un salto de línea
        {
            texto.AppendLine();
            Console.WriteLine(); // Mostrar el salto de línea en la consola
        }
        else
        {
            // Agregar el carácter al texto
            texto.Append(tecla.KeyChar);
            Console.Write(tecla.KeyChar); // Mostrar el carácter en la consola
        }
    }

    // Mostrar el texto ingresado
    Console.WriteLine("\n\nTexto ingresado:");
    Console.WriteLine(texto.ToString());


    FATSystem.ModifyFile(nombre, texto.ToString());
}
    // Funciones que servirán para llamar al sistema FAT en el programa
    static void CreateFile()
    {
        Console.Write("Ingrese el nombre del archivo: ");
        string nombre = Console.ReadLine();

        Console.Write("Ingrese el contenido del archivo: ");
        string contenido = Console.ReadLine();

        FATSystem.CreateFile(nombre, contenido);
        Console.WriteLine("Archivo creado exitosamente.");
    }

    static void OpenFile()
    {
        Console.Write("Ingrese el nombre del archivo a abrir: ");
        string nombre = Console.ReadLine();
        Console.Clear();

        FATSystem.OpenFile(nombre);
    }

    static void DeleteFile()
    {
        Console.Write("Ingrese el nombre del archivo a eliminar: ");
        string nombre = Console.ReadLine();

        FATSystem.DeleteFile(nombre);
    }
    static void RecoverFile()
    {
        Console.Write("Ingrese el nombre del archivo a recuperar: ");
        string nombre = Console.ReadLine();

        FATSystem.RecoverFile(nombre);
    }
}
