public static class StringFunctions {

    public static string Until(this string str, string chars) {
        return str.Substring(0, str.IndexOf(chars));
    }

    public static string After(this string str, string chars) {
        return str.Substring(str.IndexOf(chars) + chars.Length);
    }

    public static bool EqualsIgnoreCase(this string str, string other) {
        return str.ToLower() == other.ToLower();
    }

    public static string PlaceEnding(int number) {
        if(number == 11 || number == 12 || number == 13) return "th";

        number %= 10;
        switch(number) {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    public static int NumOccurrences(this string str, string chars) {
        return str.Length - str.Replace(chars, "").Length;
    }
}