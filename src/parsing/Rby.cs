using System.Collections.Generic;

public static class Rby {

    public static ROM ROM;
    public static SYM SYM;

    public static List<RbySpecies> Species;

    static Rby() {
        ROM = new ROM("pokeyellow");
        SYM = ROM.Symbols;

        Species = new List<RbySpecies>();
        ByteStream data = ROM.From("BaseStats");
        for(int i = 0; i < 151; i++) {
            Species.Add(new RbySpecies(data));
        }

        ByteStream pokedex = ROM.From("PokedexOrder");
        ByteStream names = ROM.From("MonsterNames");
        for(int i = 0; i < 190; i++) {
            byte id = (byte) (pokedex.u8() - 1);
            string name = Charmap.Decode(names.Read(10));
            if(id != 0xff) {
                Species[id].Name = name.FixCapitalization();
            }
        }
    }
}

public class RbySpecies {

    public string Name;
    public byte PokedexNumber;
    public byte BaseHP;
    public byte BaseAttack;
    public byte BaseDefense;
    public byte BaseSpeed;
    public byte BaseSpecial;

    public RbySpecies(ByteStream data) {
        PokedexNumber = data.u8();
        BaseHP = data.u8();
        BaseAttack = data.u8();
        BaseDefense = data.u8();
        BaseSpeed = data.u8();
        BaseSpecial = data.u8();
        data.Seek(22); // Other data is irrelevant, for now.
    }
}