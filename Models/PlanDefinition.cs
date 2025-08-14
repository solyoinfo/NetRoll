namespace NetRoll.Models
{
    public class PlanDefinition
    {
        public string Name { get; set; } = string.Empty; // pl. "FREE", "PRO"
        public long MaxStorageBytes { get; set; } // összes tárhely limit
        public int MaxFileCount { get; set; } // összes kép/fájl darab
        public int MaxProductCount { get; set; } // későbbi termék limit
    public string? HtmlDescription { get; set; } // opcionális HTML leírás
    }
}
