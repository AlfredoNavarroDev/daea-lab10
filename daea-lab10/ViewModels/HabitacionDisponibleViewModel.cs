using daea_lab10.Models;

namespace daea_lab10.ViewModels;

public class HabitacionDisponibleViewModel
{
    public Habitacion Habitacion { get; set; } = null!;

    public int Noches { get; set; }

    public decimal Total { get; set; }
}
