using daea_lab10.Models;
using daea_lab10.Repositorios;
using daea_lab10.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace daea_lab10.Controllers;

public class ReservasController : Controller
{
    private readonly ReservaRepositorio _reservaRepositorio;

    public ReservasController(ReservaRepositorio reservaRepositorio)
    {
        _reservaRepositorio = reservaRepositorio;
    }

    public async Task<IActionResult> Index()
    {
        var reservas = await _reservaRepositorio.ListarAsync();
        return View(reservas);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(int id)
    {
        var ok = await _reservaRepositorio.ConfirmarAsync(id);
        TempData["Mensaje"] = ok
            ? "Reserva confirmada correctamente."
            : "No se pudo confirmar la reserva.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var ok = await _reservaRepositorio.CancelarAsync(id);
        TempData["Mensaje"] = ok
            ? "Reserva cancelada correctamente."
            : "No se pudo cancelar la reserva.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Disponibles(DateOnly? fechaIngreso, DateOnly? fechaSalida)
    {
        var modelo = new DisponibilidadFiltroViewModel
        {
            FechaIngreso = fechaIngreso,
            FechaSalida = fechaSalida
        };

        if (fechaIngreso.HasValue && fechaSalida.HasValue)
        {
            modelo.Buscado = true;

            if (fechaSalida.Value <= fechaIngreso.Value)
            {
                ModelState.AddModelError(string.Empty, "La fecha de salida debe ser posterior a la fecha de ingreso.");
            }
            else
            {
                var habitaciones = await _reservaRepositorio.ListarDisponiblesAsync(fechaIngreso.Value, fechaSalida.Value);
                var noches = fechaSalida.Value.DayNumber - fechaIngreso.Value.DayNumber;

                modelo.Resultados = habitaciones
                    .Select(h => new HabitacionDisponibleViewModel
                    {
                        Habitacion = h,
                        Noches = noches,
                        Total = noches * h.IdTipoHabitacionNavigation.PrecioNoche
                    })
                    .ToList();
            }
        }

        return View(modelo);
    }

    public async Task<IActionResult> Create()
    {
        await CargarListasAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Reserva reserva)
    {
        ModelState.Remove("IdHuespedNavigation");
        ModelState.Remove("IdHabitacionNavigation");
        ModelState.Remove("Estado");
        ModelState.Remove("FechaRegistro");

        if (reserva.FechaSalida <= reserva.FechaIngreso)
        {
            ModelState.AddModelError("FechaSalida", "La fecha de salida debe ser posterior a la fecha de ingreso.");
        }

        if (reserva.FechaIngreso < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("FechaIngreso", "La fecha de ingreso no puede ser anterior a hoy.");
        }

        if (ModelState.IsValid &&
            await _reservaRepositorio.EstaOcupadaAsync(reserva.IdHabitacion, reserva.FechaIngreso, reserva.FechaSalida))
        {
            ModelState.AddModelError("IdHabitacion", "La habitación ya está reservada en ese rango de fechas.");
        }

        if (ModelState.IsValid)
        {
            reserva.Estado = "Pendiente";
            reserva.FechaRegistro = DateTime.Now;
            await _reservaRepositorio.AgregarAsync(reserva);
            TempData["Mensaje"] = "Reserva creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        await CargarListasAsync(reserva.IdHuesped, reserva.IdHabitacion);
        return View(reserva);
    }

    private async Task CargarListasAsync(int? huespedSeleccionado = null, int? habitacionSeleccionada = null)
    {
        var huespedes = await _reservaRepositorio.ListarHuespedesActivosAsync();
        ViewBag.Huespedes = new SelectList(
            huespedes.Select(h => new { h.IdHuesped, NombreCompleto = $"{h.Apellidos}, {h.Nombres} ({h.Dni})" }),
            "IdHuesped", "NombreCompleto", huespedSeleccionado);

        var habitaciones = await _reservaRepositorio.ListarHabitacionesActivasAsync();
        ViewBag.Habitaciones = new SelectList(
            habitaciones.Select(h => new
            {
                h.IdHabitacion,
                Descripcion = $"{h.Numero} - {h.IdTipoHabitacionNavigation.Nombre} (S/ {h.IdTipoHabitacionNavigation.PrecioNoche}/noche)"
            }),
            "IdHabitacion", "Descripcion", habitacionSeleccionada);
    }
}
