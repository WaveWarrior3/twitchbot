public static class StringFunctions {

    public static string Until(this string str, char c) {
        return str.Substring(0, str.IndexOf(c));
    }
}