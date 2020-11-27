using System;
using System.Collections.Generic;
using System.Reflection;

public static class Debug {

    public static void Log(string format, params object[] parameters) {
        Info(format, parameters);
    }

    public static void Log(object parameter) {
        Info(parameter.ToString());
    }

    public static void Info(string format, params object[] parameters) {
        LogInternal(ConsoleColor.Green, "Info", format, parameters);
    }

    public static void Warning(string format, params object[] parameters) {
        LogInternal(ConsoleColor.Yellow, "Warning", format, parameters);
    }

    public static void Error(string format, params object[] parameters) {
        LogInternal(ConsoleColor.Red, "Error", format, parameters);
    }

    public static void Assert(bool condition, string format, params object[] parameters) {
        if(!condition) Error(format, parameters);
    }

    private static void LogInternal(ConsoleColor color, string level, string format, params object[] parameters) {
        DateTime time = DateTime.Now;
        Console.ForegroundColor = color;
        Console.Write("[{0}] [{1}] ", time, level);
        Console.WriteLine(format, parameters);
    }

    public static List<(T Attribute, MethodInfo Function)> FindMethodsWithAttribute<T>() {
        var result = new List<(T Attribute, MethodInfo Function)>();

        Assembly asm = Assembly.GetExecutingAssembly();
        foreach(Type type in asm.GetTypes()) {
            foreach(MethodInfo method in type.GetMethods()) {
                object[] attributes = method.GetCustomAttributes(typeof(T), false);
                if(attributes.Length > 0) {
                    result.Add(((T) attributes[0], method));
                }
            }
        }

        return result;
    }
}