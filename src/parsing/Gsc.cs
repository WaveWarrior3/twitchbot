using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class Gsc {

    public static ROM ROM;
    public static SYM SYM;

    public static List<GscSpecies> Species;
    public static List<GscSpecies> RandSpecies;
    public static List<GscMove> Moves;
    public static List<GscMove> MetronomeMoves;

    static Gsc() {
        ROM = new ROM("pokecrystal");
        SYM = ROM.Symbols;

        Species = new List<GscSpecies>();
        ByteStream dataStream = ROM.From("BaseData");
        ByteStream nameStream = ROM.From("PokemonNames");
        for(int index = 0; index < 251; index++) {
            Species.Add(new GscSpecies(dataStream, nameStream));
        }

        List<string> illegalSpecies = new List<string>() { "Mew", "Mewtwo", "Ho-Oh", "Lugia", "Celebi", "Caterpie", "Weedle", "Metapod", "Kakuna" };
        RandSpecies = new List<GscSpecies>(Species);
        RandSpecies.RemoveAll(s => illegalSpecies.Contains(s.Name));

        Moves = new List<GscMove>();
        dataStream = ROM.From("Moves");
        nameStream = ROM.From("MoveNames");
        for(int index = 0; index <= 0xfa; index++) {
            Moves.Add(new GscMove(dataStream, nameStream));
        }

        MetronomeMoves = new List<GscMove>(Moves);
        List<byte> illegalMoves = ROM.From("MetronomeExcepts").Until(0xff).ToList();
        illegalMoves.Add(3); // Doubleslap
        illegalMoves.Add(227); // Encore
        for(int i = 0; i < illegalMoves.Count; i++) {
            MetronomeMoves.Remove(MetronomeMoves.Find(m => m.Id == illegalMoves[i]));
        }
    }
}

public class GscSpecies {

    public string Name;
    public byte Id;
    public byte BaseHP;
    public byte BaseAttack;
    public byte BaseDefense;
    public byte BaseSpeed;
    public byte BaseSpecialAttack;
    public byte BaseSpecialDefense;

    public GscSpecies(ByteStream data, ByteStream name) {
        Name = Charmap.Decode(name.Read(10)).FixCapitalization();
        Id = data.u8();
        BaseHP = data.u8();
        BaseAttack = data.u8();
        BaseDefense = data.u8();
        BaseSpeed = data.u8();
        BaseSpecialAttack = data.u8();
        BaseSpecialDefense = data.u8();
        data.Seek(25);
    }
}

public class GscMove {

    public string Name;
    public byte Id;
    public byte Effect;
    public byte Power;
    public byte Type;
    public byte Accuracy;
    public byte PP;
    public byte EffectChance;

    public GscMove(ByteStream data, ByteStream name) {
        Name = Charmap.Decode(name.Until(0x50)).FixCapitalization();
        Id = data.u8();
        Effect = data.u8();
        Power = data.u8();
        Type = data.u8();
        Accuracy = data.u8();
        PP = data.u8();
        EffectChance = data.u8();
    }
}