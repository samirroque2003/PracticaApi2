//1. Crear el servidor y los datos

var app = WebApplication.CreateBuilder(args).Build(); //crea el servidor

//Lista de memorias: son nuestras bases de datis de practica 
//al apagar la API se borran (la cuenta de prueba siemrpe vuelve)

var usuarios = new List<Usuario>
{
    new ("prueba@practica.com", "123456", "Usuraios de prueba")
};

var perfiles = new Dictionary<string, Perfil>(); // Correo -> su perfil guardado

//ventanilla de prueba : GET de la direccion raiz 
app.MapGet("/", () => "Api de practica funcionando");

//2. Ventanilla de regitro
//POST/api/auth/registro: crea una nueva ventana
// (Registro r) = los datos json que manda la app, ya converitdos a un objeto
app.MapPost("/api/auth/registro", (Registro r) =>
{
    //Quitar espacios y pasar a minusculas , para que Ana@x.com sea igual que ana@x.com
    var correo = r.Correo.Trim().ToLower();

    //validacion 1: el correo debe tener @. Responde 400 (pedido mal hecho) 
    if (!correo.Contains('@'))
    {
        return Results.BadRequest(new { mensaje = "El correo no es valido." });
    }

    //validacion 2: clave o contraseña minimo 6 caracteres
    if (r.Clave.Length < 6)
    {
        return Results.BadRequest(new { mensaje = "La contraseña es muy débil, minimo 6 caracteres." });
    }

    //validacion 3: no puede haber dos cuentas con el mismo correo va a lanzar el codigo 409
    if (usuarios.Any(u => u.Correo == correo))
    {
        return Results.Conflict(new { mensaje = "Ese correo ya esta registrado" });
    }

    usuarios.Add(new(correo, r.Clave, r.Nombre.Trim())); //guarda la cuenta
    return Results.Ok(new { mensaje = "Cuenta Creada" }); //responde 200 (ok)
});

//Parte 3. Ventanilla de login
// POST/api/auth/login: revisa los datos y entrega un token 
app.MapPost("/api/auth/login", (Login l) => {

    var correo = l.Correo.Trim().ToLower();

    //nos dara TRUE solo si hay un usuario con ese correo y esa clave

    var ok = usuarios.Any(u => u.Correo == correo && u.Clave == l.Clave);
    return ok
        ? Results.Ok(new { token = correo })
        : Results.Json(new { mensaje = "Datos incorrectos" },
                        statusCode: 401); //401 = no autorizado

});

//Parte 4. Ver de perfil (GET)
//GET/api/perfil: devuelve el perfil de quien muestra su token
app.MapGet("/api/perfil", (HttpRequest req) =>
{
    var correo = Quien(req); //leemos el token de la peticion 

    //Buscamos al usuario dueño de ese token. Si no existe: es un pase falso
    var u = usuarios.FirstOrDefault(x => x.Correo == correo);
    if (u is null)
    {
        //sin pase valido no se puede entrar 
        return Results.Json(new { mensaje = "sesion invalido o vencida" }, statusCode: 401);
    }
    return Results.Ok(perfiles.GetValueOrDefault(correo, new Perfil(u.nombre, true, 0)));
});

//Parte 5. Guardar perfil (PUT)
//PUT/api/perfil: guarda el perfil de quien muestra su token
app.MapPut("/api/perfil", (HttpRequest req, Perfil p) =>
{
    var correo = Quien(req); //leemos el token de la peticion 
    //Buscamos al usuario dueño de ese token. Si no existe: es un pase falso
    var u = usuarios.FirstOrDefault(x => x.Correo == correo);
    if (u is null)
    {
        //sin pase valido no se puede entrar 
        return Results.Json(new { mensaje = "sesion invalido o vencida" }, statusCode: 401);
    }
    perfiles[correo] = p; //guardamos el perfil
    return Results.Ok(new { mensaje = "Perfil guardado" });
});

//El pase viaja en el encabezado: Autorizacion Beares<token>
//esa funcion quita la palabra Bearer y deja el token
static String Quien(HttpRequest req) =>
       req.Headers.Authorization.ToString().Replace("Bearer ", "").Trim();


app.Run(); //arranca el servidor

//"Cajas" de datos (DTO): las forma del JSon que viaja entre app y APPIA
//van al final del archivo
record Usuario(string Correo, string Clave, string nombre);
record Registro(string Correo, string Clave, string Nombre);
record Login(string Correo, string Clave);

record Perfil(string Nombre, bool Disponible, int AniosExperiencia);