Module DigitTable

    ' A table of all two-digit numbers. This is used to speed up decimal digit
    ' generation by copying pairs of digits into the final output.
    Public DIGIT_TABLE() As Char = {
        "0"c, "0"c, "0"c, "1"c, "0"c, "2"c, "0"c, "3"c, "0"c, "4"c, "0"c,
        "5"c, "0"c, "6"c, "0"c, "7"c, "0"c, "8"c, "0"c, "9"c, "1"c, "0"c,
        "1"c, "1"c, "1"c, "2"c, "1"c, "3"c, "1"c, "4"c, "1"c, "5"c, "1"c,
        "6"c, "1"c, "7"c, "1"c, "8"c, "1"c, "9"c, "2"c, "0"c, "2"c, "1"c,
        "2"c, "2"c, "2"c, "3"c, "2"c, "4"c, "2"c, "5"c, "2"c, "6"c, "2"c,
        "7"c, "2"c, "8"c, "2"c, "9"c, "3"c, "0"c, "3"c, "1"c, "3"c, "2"c,
        "3"c, "3"c, "3"c, "4"c, "3"c, "5"c, "3"c, "6"c, "3"c, "7"c, "3"c,
        "8"c, "3"c, "9"c, "4"c, "0"c, "4"c, "1"c, "4"c, "2"c, "4"c, "3"c,
        "4"c, "4"c, "4"c, "5"c, "4"c, "6"c, "4"c, "7"c, "4"c, "8"c, "4"c,
        "9"c, "5"c, "0"c, "5"c, "1"c, "5"c, "2"c, "5"c, "3"c, "5"c, "4"c,
        "5"c, "5"c, "5"c, "6"c, "5"c, "7"c, "5"c, "8"c, "5"c, "9"c, "6"c,
        "0"c, "6"c, "1"c, "6"c, "2"c, "6"c, "3"c, "6"c, "4"c, "6"c, "5"c,
        "6"c, "6"c, "6"c, "7"c, "6"c, "8"c, "6"c, "9"c, "7"c, "0"c, "7"c,
        "1"c, "7"c, "2"c, "7"c, "3"c, "7"c, "4"c, "7"c, "5"c, "7"c, "6"c,
        "7"c, "7"c, "7"c, "8"c, "7"c, "9"c, "8"c, "0"c, "8"c, "1"c, "8"c,
        "2"c, "8"c, "3"c, "8"c, "4"c, "8"c, "5"c, "8"c, "6"c, "8"c, "7"c,
        "8"c, "8"c, "8"c, "9"c, "9"c, "0"c, "9"c, "1"c, "9"c, "2"c, "9"c,
        "3"c, "9"c, "4"c, "9"c, "5"c, "9"c, "6"c, "9"c, "7"c, "9"c, "8"c,
        "9"c, "9"c
    }

End Module
