namespace Thermal.Domain.EquipmentTypes;

/// <summary>Criterio para decidir si una lectura está fuera de límite (D-05, RN-06 y RN-07).</summary>
public enum LimitMode
{
    /// <summary>Fuera de límite si la lectura es estrictamente mayor que el máximo (refrigeración).</summary>
    Maximum,

    /// <summary>
    /// Fuera de límite si la lectura se aleja de la consigna más que la tolerancia, por arriba o por abajo
    /// (incubadoras, cámaras ambientales). La consigna se registra en cada sesión.
    /// </summary>
    Band,
}

/// <summary>Resultado de evaluar una lectura contra el límite.</summary>
public enum LimitEvaluation
{
    Within,
    Above,
    Below,
}
