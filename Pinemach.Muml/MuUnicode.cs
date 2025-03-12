namespace Pinemach.Muml;

internal static class MuUnicode {
    /// <summary>
    /// Get the total number of bytes (including the first code unit)
    /// expected in a UTF-8 code point, given the first code unit.
    /// Returns -1 for an invalid first code unit.
    /// </summary>
    internal static int GetUtf8CodePointLength(int codeUnit) {
        if(codeUnit < 0 || codeUnit > 0xff) {
            return -1;
        }
        else if((codeUnit & 0x80) == 0) {
            return 1;
        }
        else if((codeUnit & 0xe0) == 0xc0) {
            return 2;
        }
        else if((codeUnit & 0xf0) == 0xe0) {
            return 3;
        }
        else if((codeUnit & 0xf8) == 0xf0) {
            return 4;
        }
        else {
            return -1;
        }
    }
    
    internal static int DecodeUtf8CodePoint(int ch0, int ch1 = 0, int ch2 = 0, int ch3 = 0) {
        if(ch0 < 0 || ch0 > 0xff) {
            return -1;
        }
        else if((ch0 & 0x80) == 0) {
            return ch0;
        }
        else if((ch0 & 0xe0) == 0xc0) {
            return ((ch0 & 0x1f) << 6) | (ch1 & 0x3f);
        }
        else if((ch0 & 0xf0) == 0xe0) {
            return ((ch0 & 0x0f) << 12) | ((ch1 & 0x3f) << 6) | (ch2 & 0x3f);
        }
        else if((ch0 & 0xf8) == 0xf0) {
            return ((ch0 & 0x07) << 18) | ((ch1 & 0x3f) << 12) | ((ch2 & 0x3f) << 6) | (ch3 & 0x3f);
        }
        else {
            return -1;
        }
    }
    
    internal static string EncodeUtf16CodePoint(int codePoint) {
        // Not representable
        if(codePoint < 0 || codePoint > 0x10ffff) {
            return null;
        }
        // Single UTF-16 code point
        if(codePoint < 0x10000) {
            return ((char) codePoint).ToString();
        }
        // Surrogate pair
        int ch = codePoint - 0x10000;
        return (
            ((char) (0xd800 | (ch >> 10))).ToString() +
            ((char) (0xdc00 | (ch & 0x3ff))).ToString()
        );
    }
}
