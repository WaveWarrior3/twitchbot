using System.Collections.Generic;

public static class Random {

    private static System.Random Instance = new System.Random();

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