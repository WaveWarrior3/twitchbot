using System.Collections.Generic;

public static class Charmap {

    public static Dictionary<byte, string> Map = new Dictionary<byte, string>();

    static Charmap() {
        string[] legend = ("A B C D E F G H I J K L M N O P " +
                        "Q R S T U V W X Y Z ( ) : ; [ ] " +
                        "a b c d e f g h i j k l m n o p " +
                        "q r s t u v w x y z _ _ _ _ _ _ " +
                        "Ä Ö Ü ä ö ü _ _ _ _ _ _ _ _ _ _ " +
                        "'d 'l 'm 'r 's 't 'v _ _ _ _ _ _ _ _ _ " +
                        "' PM MN - _ _ ? ! . & é _ _ _ _ _MALE " +
                        "$ * . / , _FEMALE 0 1 2 3 4 5 6 7 8 9").Split(" ");

        for(int i = 0; i < legend.Length; i++) {
            Map[(byte) (0x80 + i)] = legend[i];
        }

        Map[0x7f] = " ";
    }

    public static string Decode(byte[] bytes) {
        string ret = "";
        foreach(byte b in bytes) {
            if(b == 0x50) break;
            ret += Map[b];
        }

        return ret;
    }
}