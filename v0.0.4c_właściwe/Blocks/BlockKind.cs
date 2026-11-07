//rodzaje blokow wspolne dla generatora, siatek i fizyki: przezroczyste (szklo, woda) nie zaslaniaja tego, co za nimi
public static class BlockKind
{
    public const string GlassId = "50";
    public const string Clay = "41";
    public const string Wood = "80";
    public const string Leaf = "81";
    public const string DrySand = "51";
    public const string WetSand = "52";

    public static bool IsSand(string id)
    {
        return id == DrySand || id == WetSand;
    }

    public static bool IsTransparent(string id)
    {
        return id == GlassId || Fluid.IsWater(id);
    }

    //lity i nieprzezroczysty: zaslania sciany sasiadow i swiatlo slonca
    public static bool IsOpaque(string id)
    {
        return id != null && !IsTransparent(id);
    }
}
