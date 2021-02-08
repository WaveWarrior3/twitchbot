using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

public class ROM {

    public byte[] Data;
    public SYM Symbols;

    public ROM(string name) {
        Data = File.ReadAllBytes("roms/" + name + ".gbc");
        Symbols = new SYM("sym/" + name + ".sym");
    }

    public byte this[int index] {
        get { return Data[index]; }
        set { Data[index] = value; }
    }

    public byte this[string address] {
        get { return Data[Symbols[address]]; }
        set { Data[Symbols[address]] = value; }
    }

    public ByteStream From(int offset) {
        ByteStream stream = new ByteStream(Data);
        stream.Seek(offset, SeekOrigin.Begin);
        return stream;
    }

    public ByteStream From(string address) {
        return From(Symbols[address]);
    }
}

public class ByteStream : MemoryStream {

    public bool LowerNybble;

    public ByteStream() : base() {
    }

    public ByteStream(byte[] data) : base(data) {
    }

    public byte Peek() {
        byte ret = (byte) ReadByte();
        Seek(-1, SeekOrigin.Current);
        return ret;
    }

    public void Seek(long amount) {
        Seek(amount, SeekOrigin.Current);
    }

    public byte[] Until(byte terminator) {
        int length = 0;
        do {
            length++;
        } while(ReadByte() != terminator);
        Seek(-length, SeekOrigin.Current);
        byte[] bytes = new byte[length];
        Read(bytes);
        return bytes;
    }

    public byte[] Read(int length) {
        byte[] bytes = new byte[length];
        Read(bytes);
        return bytes;
    }

    public byte Nybble() {
        byte ret = (byte) (LowerNybble ? u8() & 0xf : Peek() >> 4);
        LowerNybble = !LowerNybble;
        return ret;
    }

    public byte u8() {
        return (byte) ReadByte();
    }

    public ushort u16le() {
        return (ushort) (ReadByte() | (ReadByte() << 8));
    }

    public ushort u16be() {
        return (ushort) ((ReadByte() << 8) | ReadByte());
    }

    public int u24le() {
        return (int) (ReadByte() | (ReadByte() << 8) | (ReadByte() << 16));
    }

    public int u24be() {
        return (int) ((ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
    }

    public uint u32le() {
        return (uint) (ReadByte() | (ReadByte() << 8) | (ReadByte() << 16) | (ReadByte() << 24));
    }

    public uint u32be() {
        return (uint) ((ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte());
    }
}

public class SYM : Dictionary<string, int> {

    public SYM(string file) : base() {
        string[] lines = File.ReadAllLines(file);
        for(int i = 0; i < lines.Length; i++) {
            string line = lines[i].Trim();
            if(line.StartsWith(";")) continue;

            Match match = Regex.Match(line, "([0-9a-fA-F]+):([0-9a-fA-F]+) (.+)");
            byte bank = Convert.ToByte(match.Groups[1].Value, 16);
            ushort addr = Convert.ToUInt16(match.Groups[2].Value, 16);
            string label = match.Groups[3].Value;
            Add(label, bank * 0x4000 + (addr - 0x4000));
        }
    }
}