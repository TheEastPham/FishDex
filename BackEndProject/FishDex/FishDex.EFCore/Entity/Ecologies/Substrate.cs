namespace FishDex.EFCore.Entity.Ecologies;

public class Substrate
{
    public int SubstrateId { get; set; }
    public int EcologyId { get; set; }
    public string? SubstrateRef { get; set; }
    public bool Benthic { get; set; }
    public bool Sessile { get; set; }
    public bool Mobile { get; set; }
    public bool Demersal { get; set; }
    public bool Endofauna { get; set; }
    public bool Pelagic { get; set; }
    public bool Megabenthos { get; set; }
    public bool Macrobenthos { get; set; }
    public bool Meiobenthos { get; set; }
    public bool SoftBottom { get; set; }
    public bool Sand { get; set; }
    public bool Coarse { get; set; }
    public bool Fine { get; set; }
    public bool Level { get; set; }
    public bool Sloping { get; set; }
    public bool Silt { get; set; }
    public bool Mud { get; set; }
    public bool Ooze { get; set; }
    public bool Detritus { get; set; }
    public bool Organic { get; set; }
    public bool HardBottom { get; set; }
    public bool Rocky { get; set; }
    public bool Rubble { get; set; }
    public bool Gravel { get; set; }

    public Ecology Ecology { get; set; }
}
