using daea_lab10.Models;
using Microsoft.EntityFrameworkCore;

namespace daea_lab10.Repositorios;

public class ReservaRepositorio
{
    private readonly HotelContext _context;

    public ReservaRepositorio(HotelContext context)
    {
        _context = context;
    }

    public async Task<List<Reserva>> ListarAsync()
    {
        return await _context.Reservas
            .AsNoTracking()
            .Include(r => r.IdHuespedNavigation)
            .Include(r => r.IdHabitacionNavigation).ThenInclude(h => h.IdTipoHabitacionNavigation)
            .OrderByDescending(r => r.FechaRegistro)
            .ToListAsync();
    }

    public async Task<List<Huesped>> ListarHuespedesActivosAsync()
    {
        return await _context.Huespeds
            .AsNoTracking()
            .Where(h => h.Activo)
            .OrderBy(h => h.Apellidos).ThenBy(h => h.Nombres)
            .ToListAsync();
    }

    public async Task<List<Habitacion>> ListarHabitacionesActivasAsync()
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo)
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    public async Task<List<Habitacion>> ListarDisponiblesAsync(DateOnly ingreso, DateOnly salida)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo && !h.Reservas.Any(r =>
                r.Estado != "Cancelada" &&
                r.FechaIngreso < salida &&
                r.FechaSalida > ingreso))
            .OrderBy(h => h.Numero)
            .ToListAsync();
    }

    public async Task<bool> EstaOcupadaAsync(int idHabitacion, DateOnly ingreso, DateOnly salida)
    {
        return await _context.Reservas.AnyAsync(r =>
            r.IdHabitacion == idHabitacion &&
            r.Estado != "Cancelada" &&
            r.FechaIngreso < salida &&
            r.FechaSalida > ingreso);
    }

    public async Task AgregarAsync(Reserva reserva)
    {
        _context.Reservas.Add(reserva);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ConfirmarAsync(int idReserva)
    {
        var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.IdReserva == idReserva);
        if (reserva is null || reserva.Estado != "Pendiente")
        {
            return false;
        }

        reserva.Estado = "Confirmada";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CancelarAsync(int idReserva)
    {
        var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.IdReserva == idReserva);
        if (reserva is null || (reserva.Estado != "Pendiente" && reserva.Estado != "Confirmada"))
        {
            return false;
        }

        reserva.Estado = "Cancelada";
        await _context.SaveChangesAsync();
        return true;
    }
}
