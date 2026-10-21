# Hotel.Web — Reservas de Hotel (EF Core Database First)

Aplicación web ASP.NET Core MVC para gestión de habitaciones y reservas de un hotel, construida con Entity Framework Core en modalidad **Database First** (sin migraciones, sin procedimientos almacenados).

## Enunciado del desafío

Implementar una app de reservas de hotel con las siguientes reglas:

- Base de datos `HotelDB` creada a partir de un script SQL (`HotelDB.sql`) entregado por el docente, con las tablas `TipoHabitacion`, `Habitacion`, `Huesped` y `Reserva`, más datos de prueba.
- Una `Reserva` une un `Huesped` con una `Habitacion` y tiene `FechaIngreso`, `FechaSalida` y `Estado` (`Pendiente`, `Confirmada`, `Cancelada`, `Finalizada`).
- `Habitacion` y `Huesped` usan **eliminación lógica** (`Activo = 0`), nunca `DELETE` físico.
- Proyecto ASP.NET Core Web App (MVC) llamado `daea-lab10`, con paquetes `Microsoft.EntityFrameworkCore.SqlServer`, `.Design` y `.Tools`.
- Modelo generado con **scaffold** en `Models/`, contexto `HotelContext`, cadena de conexión en `appsettings.json`, registrado con `AddDbContext`, sin `OnConfiguring` en el contexto.
- Acceso a datos encapsulado en `Repositorios/` (`HabitacionRepositorio`, `ReservaRepositorio`) — los controladores nunca usan `HotelContext` directamente.
- CRUD de habitaciones, disponibilidad de habitaciones por rango de fechas, y gestión completa del ciclo de vida de una reserva.
- Todo el acceso a datos es **async** (`ToListAsync`, `AnyAsync`, `SaveChangesAsync`), con `AsNoTracking()` en lecturas y sin SQL escrito a mano.

## Stack

- .NET 10 / ASP.NET Core MVC
- Entity Framework Core 10 (SqlServer, Design, Tools)
- SQL Server (Express), Database First vía `dotnet ef dbcontext scaffold`
- Bootstrap (vistas por defecto del template MVC)

## Estructura del proyecto

```
daea-lab10/
├── HotelDB.sql                     # Script de creación de la BD (fuera del proyecto .csproj)
└── daea-lab10/
    ├── Models/                     # Generado por scaffold — NO modificar a mano
    │   ├── HotelContext.cs
    │   ├── Habitacion.cs
    │   ├── Huesped.cs
    │   ├── Reserva.cs
    │   └── TipoHabitacion.cs
    ├── Repositorios/
    │   ├── HabitacionRepositorio.cs
    │   └── ReservaRepositorio.cs
    ├── ViewModels/
    │   ├── HabitacionDisponibleViewModel.cs
    │   └── DisponibilidadFiltroViewModel.cs
    ├── Controllers/
    │   ├── HabitacionesController.cs
    │   └── ReservasController.cs
    ├── Views/
    │   ├── Habitaciones/           # Index, Details, Create, Edit, Delete
    │   ├── Reservas/               # Index, Create, Disponibles
    │   └── Shared/_Layout.cshtml   # Menú: Habitaciones / Disponibilidad / Reservas
    ├── appsettings.json            # ConnectionStrings:HotelContext
    └── Program.cs                  # AddDbContext + AddScoped de repositorios
```

## Base de datos

Ejecutado con `sqlcmd` contra una instancia local `.\SQLEXPRESS`:

```
sqlcmd -S ".\SQLEXPRESS" -E -i "HotelDB.sql"
```

Esto crea `HotelDB` con 4 tablas (`TipoHabitacion`, `Habitacion`, `Huesped`, `Reserva`), restricciones (`CHECK`, `UNIQUE`, `FK`), un índice compuesto `IX_Reserva_Habitacion_Fechas`, y datos de prueba (incluyendo una habitación y un huésped inactivos para probar la baja lógica, y reservas con fechas relativas a `GETDATE()`).

Al final del desarrollo se aplicó el cambio incremental pedido por el docente:

```sql
ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL;
```

## Cadena de conexión

En `appsettings.json`:

```json
"ConnectionStrings": {
  "HotelContext": "Server=.\\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Registrada en `Program.cs`:

```csharp
builder.Services.AddDbContext<HotelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HotelContext")));

builder.Services.AddScoped<HabitacionRepositorio>();
builder.Services.AddScoped<ReservaRepositorio>();
```

## Comandos de scaffold utilizados

Instalación de la herramienta (una sola vez):

```
dotnet tool install --global dotnet-ef
```

Scaffold inicial del modelo:

```
dotnet ef dbcontext scaffold "Server=.\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext
```

Re-scaffold tras agregar la columna `Observaciones`:

```
dotnet ef dbcontext scaffold "Server=.\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext --force
```

> Después de cada scaffold se eliminó a mano el método `OnConfiguring` generado en `HotelContext.cs`, ya que la cadena de conexión vive en `appsettings.json` y se inyecta vía `AddDbContext` (regla del enunciado).

## Funcionalidades implementadas

### Habitaciones (`HabitacionesController`)

- **Index**: lista habitaciones activas con su tipo, filtro por `IdTipoHabitacion` y orden por número (asc/desc).
- **Details**: ficha de una habitación con su tipo, capacidad y precio.
- **Create / Edit**: formulario con `<select>` de tipos de habitación activos; si el número ya existe, error vía `ModelState.AddModelError("Numero", ...)`.
- **Delete**: baja lógica (`Activo = false`), nunca `DELETE` físico.
- Todas las acciones POST siguen el patrón **Post/Redirect/Get** con confirmación vía `TempData["Mensaje"]`.

### Reservas (`ReservasController`)

- **Disponibles** (GET con filtro por `fechaIngreso`/`fechaSalida`): devuelve habitaciones activas libres en ese rango, con noches y total estimado. El cruce de fechas se resuelve en el repositorio con una sola consulta LINQ (`!Any`): una reserva bloquea si `Estado != "Cancelada"` y `FechaIngreso < salidaPedida` y `FechaSalida > ingresoPedido`.
- **Create**: `<select>` de huéspedes y habitaciones activas. Validaciones vía `ModelState.AddModelError`:
  - `FechaSalida` debe ser posterior a `FechaIngreso`.
  - `FechaIngreso` no puede ser anterior a hoy.
  - La habitación no debe estar reservada en ese rango.
  - Se guarda con `Estado = "Pendiente"`.
  - Incluye el campo `Observaciones` (agregado tras el `ALTER TABLE`).
- **Index**: lista reservas con huésped, habitación, fechas, estado y total. Permite:
  - **Confirmar** una reserva `Pendiente` → pasa a `Confirmada`.
  - **Cancelar** una reserva `Pendiente` o `Confirmada` → pasa a `Cancelada`.

### Menú (`_Layout.cshtml`)

Navbar con **Habitaciones**, **Disponibilidad** y **Reservas**. Se removieron los enlaces por defecto de **Privacy** (nav y footer) del template base de ASP.NET Core.

## Decisiones de diseño

- **Repositorios** (`HabitacionRepositorio`, `ReservaRepositorio`) concentran todas las consultas LINQ y los `SaveChangesAsync`, incluyendo los datos de los `<select>`. Se inyectan por constructor y se registran con `AddScoped`. Los controladores nunca tocan `HotelContext` directamente.
- Las lecturas usan `AsNoTracking()`; las operaciones que modifican una entidad (editar, dar de baja, cancelar, confirmar) primero la obtienen con tracking para poder actualizarla y llamar `SaveChangesAsync()`.
- Las vistas de `Create`/`Edit` usan `ModelState.Remove(...)` sobre las propiedades de navegación (`IdTipoHabitacionNavigation`, `Reservas`, `IdHuespedNavigation`, `IdHabitacionNavigation`) para que la validación del modelo generado por scaffold no falle al no venir esas propiedades desde el formulario.
- El total de una reserva/disponibilidad se calcula como `noches * PrecioNoche` del tipo de habitación, nunca se persiste — se calcula al vuelo.

## Cómo ejecutar

1. Tener una instancia de SQL Server accesible (en este proyecto: `.\SQLEXPRESS`, el servicio `MSSQL$SQLEXPRESS` debe estar corriendo).
2. Ejecutar `HotelDB.sql` contra esa instancia.
3. Ajustar `ConnectionStrings:HotelContext` en `appsettings.json` si el nombre de instancia es distinto.
4. Desde `daea-lab10/daea-lab10/`:
   ```
   dotnet build
   dotnet run
   ```
5. Abrir la URL indicada por `dotnet run` (ej. `http://localhost:5280`) y navegar por **Habitaciones**, **Disponibilidad** y **Reservas**.

## Verificación manual realizada

- Crear habitación nueva y validar rechazo de número duplicado.
- Crear reserva, ver total calculado, confirmar y cancelar desde el listado.
- Filtrar disponibilidad por rango de fechas y verificar que excluye habitaciones ocupadas o inactivas.
- Confirmar persistencia del campo `Observaciones` directamente en la base de datos tras el `ALTER TABLE`.
