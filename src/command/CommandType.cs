public enum CommandType {

    None,
    SystemCommand,
    CustomCommand,
    Alias,
}

public static class CommandTypeFunctions {

    public static string Format(this CommandType type) {
        switch(type) {
            case CommandType.SystemCommand: return "a system command";
            case CommandType.CustomCommand: return "a custom command";
            case CommandType.Alias: return "an alias";
            default: return "";
        }
    }
}