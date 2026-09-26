namespace Thermal.Domain.EquipmentTypes;

/// <summary>Criterio para decidir si una lectura está fuera de límite (D-05, D-07, RN-06 y RN-07).</summary>
public enum LimitMode
{
    /// <summary>
    /// Límites absolutos: fuera de límite si la lectura es estrictamente mayor que el máximo o menor que el mínimo;
    /// cualquiera de los dos puede faltar (refrigeradora de +2 a +8 °C; congeladora de plasma o ultracongeladora:
    /// solo máximo).
    /// </summary>
    Range,

    /// <summary>
    /// Relativo a la consigna: fuera de límite si la lectura se aleja de la consigna más que la tolerancia, por
    /// arriba o por abajo (incubadoras, cámaras ambientales). La consigna se registra en cada sesión.
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
