using System;
using System.Collections.Generic;

public static class Random {

    private static System.Random Instance = new WELL512();

    public static int Next() {
        return Instance.Next();
    }

    public static int Next(int max) {
        return Instance.Next(max);
    }

    public static int Next(int min, int max) {
        return Instance.Next(min, max);
    }

    public static double NextDouble() {
        return Instance.NextDouble();
    }

    public static T Next<T>(T[] arr) {
        return arr[Next(arr.Length)];
    }

    public static T Next<T>(List<T> list) {
        return list[Next(list.Count)];
    }
}

public class WELL512 : System.Random {

    private long[] State = new long[16];
    private int Index = 0;

    public WELL512() : this(Environment.TickCount64) {
    }

    public WELL512(long seed) {
        seed = Math.Abs(seed);
        for(int i = 0; i < State.Length; i++) {
            State[i] = (seed + 1) * ((seed + 1) << 2) * i;
        }
    }

    public override int Next() {
        long a = State[Index];
        long b = State[(Index - 3) & 0xf];
        long c = a ^ b ^ ((b ^ 2 * a) << 15);
        long d = (State[(Index - 7) & 0xf] >> 11) ^ State[(Index - 7) & 0xf] ^ c;
        long e = d ^ c;
        State[Index] = e;
        long f = e ^ 0x20 * (e & 0xfed22169);
        Index = (Index - 1) & 15;
        long g = State[Index];
        long h = f ^ 4 * (g ^ ((c ^ (d << 10)) << 16));
        State[Index] = g ^ c ^ h;
        return (int) State[Index];

    }
}