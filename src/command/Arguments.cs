using System;
using System.Text;
using System.Text.RegularExpressions;

public class Arguments {

    public ArraySegment<string> Args;
    public string FullString;

    public string this[int index] {
        get {
            return index >= Args.Count ? "" : Args[index];
        }
        set {
            Args[index] = value;
        }
    }

    public string Join(int startIndex, string separator) {
        StringBuilder sb = new StringBuilder();
        for(int i = startIndex; i < Args.Count; i++) {
            sb.Append(Args[i]);
            if(i != Args.Count - 1) sb.Append(separator);
        }
        return sb.ToString();
    }

    public bool TryInt(int index, out int ret) {
        return int.TryParse(Args[index], out ret);
    }

    public bool TryFloat(int index, out float ret) {
        return float.TryParse(Args[index], out ret);
    }

    public int Int(int index) {
        return int.Parse(Args[index]);
    }

    public float TryFloat(int index) {
        return float.Parse(Args[index]);
    }

    public int Length() {
        return Args.Count;
    }

    public bool Matches(string pattern) {
        return Regex.IsMatch(FullString.ToLower(), pattern);
    }
}