Module DigitTable
    ' A table of all two-digit numbers. This is used to speed up decimal digit
    ' generation by copying pairs of digits into the final output.
    Friend ReadOnly DIGIT_TABLE() As SByte = {
        AscW("0"c), AscW("0"c), AscW("0"c), AscW("1"c), AscW("0"c), AscW("2"c), AscW("0"c),
        AscW("3"c), AscW("0"c), AscW("4"c), AscW("0"c), AscW("5"c), AscW("0"c), AscW("6"c),
        AscW("0"c), AscW("7"c), AscW("0"c), AscW("8"c), AscW("0"c), AscW("9"c), AscW("1"c),
        AscW("0"c), AscW("1"c), AscW("1"c), AscW("1"c), AscW("2"c), AscW("1"c), AscW("3"c),
        AscW("1"c), AscW("4"c), AscW("1"c), AscW("5"c), AscW("1"c), AscW("6"c), AscW("1"c),
        AscW("7"c), AscW("1"c), AscW("8"c), AscW("1"c), AscW("9"c), AscW("2"c), AscW("0"c),
        AscW("2"c), AscW("1"c), AscW("2"c), AscW("2"c), AscW("2"c), AscW("3"c), AscW("2"c),
        AscW("4"c), AscW("2"c), AscW("5"c), AscW("2"c), AscW("6"c), AscW("2"c), AscW("7"c),
        AscW("2"c), AscW("8"c), AscW("2"c), AscW("9"c), AscW("3"c), AscW("0"c), AscW("3"c),
        AscW("1"c), AscW("3"c), AscW("2"c), AscW("3"c), AscW("3"c), AscW("3"c), AscW("4"c),
        AscW("3"c), AscW("5"c), AscW("3"c), AscW("6"c), AscW("3"c), AscW("7"c), AscW("3"c),
        AscW("8"c), AscW("3"c), AscW("9"c), AscW("4"c), AscW("0"c), AscW("4"c), AscW("1"c),
        AscW("4"c), AscW("2"c), AscW("4"c), AscW("3"c), AscW("4"c), AscW("4"c), AscW("4"c),
        AscW("5"c), AscW("4"c), AscW("6"c), AscW("4"c), AscW("7"c), AscW("4"c), AscW("8"c),
        AscW("4"c), AscW("9"c), AscW("5"c), AscW("0"c), AscW("5"c), AscW("1"c), AscW("5"c),
        AscW("2"c), AscW("5"c), AscW("3"c), AscW("5"c), AscW("4"c), AscW("5"c), AscW("5"c),
        AscW("5"c), AscW("6"c), AscW("5"c), AscW("7"c), AscW("5"c), AscW("8"c), AscW("5"c),
        AscW("9"c), AscW("6"c), AscW("0"c), AscW("6"c), AscW("1"c), AscW("6"c), AscW("2"c),
        AscW("6"c), AscW("3"c), AscW("6"c), AscW("4"c), AscW("6"c), AscW("5"c), AscW("6"c),
        AscW("6"c), AscW("6"c), AscW("7"c), AscW("6"c), AscW("8"c), AscW("6"c), AscW("9"c),
        AscW("7"c), AscW("0"c), AscW("7"c), AscW("1"c), AscW("7"c), AscW("2"c), AscW("7"c),
        AscW("3"c), AscW("7"c), AscW("4"c), AscW("7"c), AscW("5"c), AscW("7"c), AscW("6"c),
        AscW("7"c), AscW("7"c), AscW("7"c), AscW("8"c), AscW("7"c), AscW("9"c), AscW("8"c),
        AscW("0"c), AscW("8"c), AscW("1"c), AscW("8"c), AscW("2"c), AscW("8"c), AscW("3"c),
        AscW("8"c), AscW("4"c), AscW("8"c), AscW("5"c), AscW("8"c), AscW("6"c), AscW("8"c),
        AscW("7"c), AscW("8"c), AscW("8"c), AscW("8"c), AscW("9"c), AscW("9"c), AscW("0"c),
        AscW("9"c), AscW("1"c), AscW("9"c), AscW("2"c), AscW("9"c), AscW("3"c), AscW("9"c),
        AscW("4"c), AscW("9"c), AscW("5"c), AscW("9"c), AscW("6"c), AscW("9"c), AscW("7"c),
        AscW("9"c), AscW("8"c), AscW("9"c), AscW("9"c)
    }
End Module
