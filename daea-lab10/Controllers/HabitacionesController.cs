using daea_lab10.Models;
using daea_lab10.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace daea_lab10.Controllers;

public class HabitacionesController : Controller
{
    private readonly HabitacionRepositorio _habitacionRepositorio;

    public HabitacionesController(HabitacionRepositorio habitacionRepositorio)
    {
        _habitacionRepositorio = habitacionRepositorio;
    }

    public async Task<IActionResult> Index(int? idTipoHabitacion, string orden = "numero_asc")
    {
        var tipos = await _habitacionRepositorio.ListarTiposActivosAsync();
        ViewBag.Tipos = new SelectList(tipos, "IdTipoHabitacion", "Nombre", idTipoHabitacion);
        ViewBag.IdTipoHabitacion = idTipoHabitacion;
        ViewBag.Orden = orden;

        var habitaciones = await _habitacionRepositorio.ListarActivasAsync(idTipoHabitacion, orden);
        return View(habitaciones);
    }

    public async Task<IActionResult> Details(int id)
    {
        var habitacion = await _habitacionRepositorio.ObtenerPorIdAsync(id);
        if (habitacion is null)
        {
            return NotFound();
        }

        return View(habitacion);
    }

    public async Task<IActionResult> Create()
    {
        await CargarTiposAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Habitacion habitacion)
    {
        ModelState.Remove("IdTipoHabitacionNavigation");
        ModelState.Remove("Reservas");

        if (await _habitacionRepositorio.ExisteNumeroAsync(habitacion.Numero))
        {
            ModelState.AddModelError("Numero", "Ya existe una habitación con ese número.");
        }

        if (ModelState.IsValid)
        {
            habitacion.Activo = true;
            await _habitacionRepositorio.AgregarAsync(habitacion);
            TempData["Mensaje"] = "Habitación creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var habitacion = await _habitacionRepositorio.ObtenerParaEditarAsync(id);
        if (habitacion is null)
        {
            return NotFound();
        }

        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Habitacion habitacion)
    {
        if (id != habitacion.IdHabitacion)
        {
            return NotFound();
        }

        ModelState.Remove("IdTipoHabitacionNavigation");
        ModelState.Remove("Reservas");

        if (await _habitacionRepositorio.ExisteNumeroAsync(habitacion.Numero, habitacion.IdHabitacion))
        {
            ModelState.AddModelError("Numero", "Ya existe una habitación con ese número.");
        }

        if (ModelState.IsValid)
        {
            await _habitacionRepositorio.ActualizarAsync(habitacion);
            TempData["Mensaje"] = "Habitación actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        await CargarTiposAsync(habitacion.IdTipoHabitacion);
        return View(habitacion);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var habitacion = await _habitacionRepositorio.ObtenerPorIdAsync(id);
        if (habitacion is null)
        {
            return NotFound();
        }

        return View(habitacion);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _habitacionRepositorio.EliminarLogicoAsync(id);
        TempData["Mensaje"] = "Habitación eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarTiposAsync(int? seleccionado = null)
    {
        var tipos = await _habitacionRepositorio.ListarTiposActivosAsync();
        ViewBag.Tipos = new SelectList(tipos, "IdTipoHabitacion", "Nombre", seleccionado);
    }
}
