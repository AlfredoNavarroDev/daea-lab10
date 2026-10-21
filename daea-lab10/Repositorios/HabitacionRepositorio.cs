using daea_lab10.Models;
using Microsoft.EntityFrameworkCore;

namespace daea_lab10.Repositorios;

public class HabitacionRepositorio
{
    private readonly HotelContext _context;

    public HabitacionRepositorio(HotelContext context)
    {
        _context = context;
    }

    public async Task<List<Habitacion>> ListarActivasAsync(int? idTipoHabitacion, string orden)
    {
        var query = _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .Where(h => h.Activo);

        if (idTipoHabitacion.HasValue)
        {
            query = query.Where(h => h.IdTipoHabitacion == idTipoHabitacion.Value);
        }

        query = orden switch
        {
            "numero_desc" => query.OrderByDescending(h => h.Numero),
            _ => query.OrderBy(h => h.Numero)
        };

        return await query.ToListAsync();
    }

    public async Task<Habitacion?> ObtenerPorIdAsync(int id)
    {
        return await _context.Habitacions
            .AsNoTracking()
            .Include(h => h.IdTipoHabitacionNavigation)
            .FirstOrDefaultAsync(h => h.IdHabitacion == id);
    }

    public async Task<Habitacion?> ObtenerParaEditarAsync(int id)
    {
        return await _context.Habitacions
            .FirstOrDefaultAsync(h => h.IdHabitacion == id);
    }

    public async Task<bool> ExisteNumeroAsync(string numero, int? idHabitacionExcluir = null)
    {
        return await _context.Habitacions
            .AnyAsync(h => h.Numero == numero && h.IdHabitacion != (idHabitacionExcluir ?? 0));
    }

    public async Task<List<TipoHabitacion>> ListarTiposActivosAsync()
    {
        return await _context.TipoHabitacions
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    public async Task AgregarAsync(Habitacion habitacion)
    {
        _context.Habitacions.Add(habitacion);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Habitacion habitacion)
    {
        _context.Habitacions.Update(habitacion);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> EliminarLogicoAsync(int id)
    {
        var habitacion = await _context.Habitacions.FirstOrDefaultAsync(h => h.IdHabitacion == id);
        if (habitacion is null)
        {
            return false;
        }

        habitacion.Activo = false;
        await _context.SaveChangesAsync();
        return true;
    }
}
