using System;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SystemCommand : Attribute {

    public string Name;
    public int MinArguments;

    public SystemCommand(string name, int minArguments = 0) {
        Name = name;
        MinArguments = minArguments;
    }
}