namespace daea_lab10.ViewModels;

public class DisponibilidadFiltroViewModel
{
    public DateOnly? FechaIngreso { get; set; }

    public DateOnly? FechaSalida { get; set; }

    public List<HabitacionDisponibleViewModel> Resultados { get; set; } = new();

    public bool Buscado { get; set; }
}
