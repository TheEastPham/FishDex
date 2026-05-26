using System;
using System.Collections.Generic;

namespace FishDex.EFCore.Entity.Species;

public class Family
{
    public Guid Id { get; set; }
    public int FamCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CommonName { get; set; }
    public string? BodyShapeI { get; set; }
    public string? SwimMode { get; set; }
    public string? ReproductiveGuild { get; set; }

    public virtual ICollection<Genus> Genuses { get; set; }
    public virtual ICollection<Species> Species { get; set; }
}
