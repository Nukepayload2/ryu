Imports System
Imports System.Numerics

' Copyright 2018 Ulf Adams
'
' Licensed under the Apache License, Version 2.0 (the "License");
' you may not use this file except in compliance with the License.
' You may obtain a copy of the License at
'
'     http://www.apache.org/licenses/LICENSE-2.0
'
' Unless required by applicable law or agreed to in writing, software
' distributed under the License is distributed on an "AS IS" BASIS,
' WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
' See the License for the specific language governing permissions and
' limitations under the License.

''' <summary>
''' An implementation of Ryu for double.
''' </summary>
Public NotInheritable Class RyuDouble
    Private Shared DEBUG As Boolean = False

    Private Const DOUBLE_MANTISSA_BITS As Integer = 52
    Private Shared ReadOnly DOUBLE_MANTISSA_MASK As Long = (1L << DOUBLE_MANTISSA_BITS) - 1

    Private Const DOUBLE_EXPONENT_BITS As Integer = 11
    Private Shared ReadOnly DOUBLE_EXPONENT_MASK As Integer = (1 << DOUBLE_EXPONENT_BITS) - 1
    Private Shared ReadOnly DOUBLE_EXPONENT_BIAS As Integer = (1 << (DOUBLE_EXPONENT_BITS - 1)) - 1

    Private Const POS_TABLE_SIZE As Integer = 326
    Private Const NEG_TABLE_SIZE As Integer = 291

    ' Only for debugging.
    Private Shared ReadOnly POW5(POS_TABLE_SIZE - 1) As BigInteger
    Private Shared ReadOnly POW5_INV(NEG_TABLE_SIZE - 1) As BigInteger

    Private Const POW5_BITCOUNT As Integer = 121 ' max 3*31 = 124
    Private Const POW5_QUARTER_BITCOUNT As Integer = 31
    'JAVA TO VB CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
    'ORIGINAL LINE: Private Shared ReadOnly POW5_SPLIT[][] As Integer = new Integer[POS_TABLE_SIZE][4]
    Private Shared ReadOnly POW5_SPLIT()() As Integer = RectangularIntegerArray(POS_TABLE_SIZE, 4)

    Private Const POW5_INV_BITCOUNT As Integer = 122 ' max 3*31 = 124
    Private Const POW5_INV_QUARTER_BITCOUNT As Integer = 31
    'JAVA TO VB CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
    'ORIGINAL LINE: Private Shared ReadOnly POW5_INV_SPLIT[][] As Integer = new Integer[NEG_TABLE_SIZE][4]
    Private Shared ReadOnly POW5_INV_SPLIT()() As Integer = RectangularIntegerArray(NEG_TABLE_SIZE, 4)

    Shared Sub New()
        Dim mask As BigInteger = New BigInteger(1) << (POW5_QUARTER_BITCOUNT - BigInteger.One)
        Dim invMask As BigInteger = New BigInteger(1) << (POW5_INV_QUARTER_BITCOUNT - BigInteger.One)
        For i As Integer = 0 To Math.Max(POW5.Length, POW5_INV.Length) - 1
            Dim pow As BigInteger = BigInteger.Pow(5, i)
            Dim pow5len As Integer = i.GetPow5Bits
            Dim expectedPow5Bits As Integer = pow5bits(i)
            If expectedPow5Bits <> pow5len Then
                Throw New InvalidOperationException(pow5len & " != " & expectedPow5Bits)
            End If
            If i < POW5.Length Then
                POW5(i) = pow
            End If
            If i < POW5_SPLIT.Length Then
                For j As Integer = 0 To 3
                    POW5_SPLIT(i)(j) = pow >> (pow5len - POW5_BITCOUNT + (3 - j) * POW5_QUARTER_BITCOUNT) And mask
                Next j
            End If

            If i < POW5_INV_SPLIT.Length Then
                ' We want floor(log_2 5^q) here, which is pow5len - 1.
                Dim j As Integer = pow5len - 1 + POW5_INV_BITCOUNT
                Dim inv As BigInteger = (BigInteger.One << j / pow) + BigInteger.One
                POW5_INV(i) = inv
                For k As Integer = 0 To 3
                    If k = 0 Then
                        POW5_INV_SPLIT(i)(k) = inv >> ((3 - k) * POW5_INV_QUARTER_BITCOUNT)
                    Else
                        POW5_INV_SPLIT(i)(k) = inv >> ((3 - k) * POW5_INV_QUARTER_BITCOUNT) And (invMask)
                    End If
                Next k
            End If
        Next i
    End Sub

    Public Shared Function doubleToString(ByVal value As Double) As String
        Return doubleToString(value, RoundingMode.ROUND_EVEN)
    End Function

    Public Shared Function doubleToString(ByVal value As Double, ByVal roundingMode As RoundingMode) As String
        ' Step 1: Decode the floating point number, and unify normalized and subnormal cases.
        ' First, handle all the trivial cases.
        If Double.IsNaN(value) Then
            Return "NaN"
        End If
        If value = Double.PositiveInfinity Then
            Return "Infinity"
        End If
        If value = Double.NegativeInfinity Then
            Return "-Infinity"
        End If
        Dim bits As Long = BitConverter.DoubleToInt64Bits(value)
        If bits = 0 Then
            Return "0.0"
        End If
        If bits = &H8000000000000000L Then
            Return "-0.0"
        End If

        ' Otherwise extract the mantissa and exponent bits and run the full algorithm.
        Dim ieeeExponent As Integer = CInt((CLng(CULng(bits) >> DOUBLE_MANTISSA_BITS)) And DOUBLE_EXPONENT_MASK)
        Dim ieeeMantissa As Long = bits And DOUBLE_MANTISSA_MASK
        Dim e2 As Integer
        Dim m2 As Long
        If ieeeExponent = 0 Then
            ' Denormal number - no implicit leading 1, and the exponent is 1, not 0.
            e2 = 1 - DOUBLE_EXPONENT_BIAS - DOUBLE_MANTISSA_BITS
            m2 = ieeeMantissa
        Else
            ' Add implicit leading 1.
            e2 = ieeeExponent - DOUBLE_EXPONENT_BIAS - DOUBLE_MANTISSA_BITS
            m2 = ieeeMantissa Or (1L << DOUBLE_MANTISSA_BITS)
        End If

        Dim sign As Boolean = bits < 0
        If DEBUG Then
            Console.WriteLine("IN=" & bits.ToString("X"))
            Console.WriteLine("   S=" & If(sign, "-", "+") & " E=" & e2 & " M=" & m2)
        End If

        ' Step 2: Determine the interval of legal decimal representations.
        Dim even As Boolean = (m2 And 1) = 0
        'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
        'ORIGINAL LINE: final long mv = 4 * m2;
        Dim mv As Long = 4 * m2
        'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
        'ORIGINAL LINE: final long mp = 4 * m2 + 2;
        Dim mp As Long = 4 * m2 + 2
        'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
        'ORIGINAL LINE: final int mmShift = ((m2 != (1L << DOUBLE_MANTISSA_BITS)) || (ieeeExponent <= 1)) ? 1 : 0;
        Dim mmShift As Integer = If((m2 <> (1L << DOUBLE_MANTISSA_BITS)) OrElse (ieeeExponent <= 1), 1, 0)
        'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
        'ORIGINAL LINE: final long mm = 4 * m2 - 1 - mmShift;
        Dim mm As Long = 4 * m2 - 1 - mmShift
        e2 -= 2

        Dim e10 As Integer
        If DEBUG Then
            Dim sv, sp, sm As String
            If e2 >= 0 Then
                sv = (New BigInteger(mv) << e2).ToString
                sp = (New BigInteger(mp) << e2).ToString
                sm = (New BigInteger(mm) << e2).ToString
                e10 = 0
            Else
                Dim factor As BigInteger = BigInteger.Pow(5, -e2)
                sv = (New BigInteger(mv) * factor).ToString()
                sp = (New BigInteger(mp) * factor).ToString()
                sm = (New BigInteger(mm) * factor).ToString()
                e10 = e2
            End If

            e10 += sp.Length - 1

            Console.WriteLine("E =" & e10)
            Console.WriteLine("d+=" & sp)
            Console.WriteLine("d =" & sv)
            Console.WriteLine("d-=" & sm)
            Console.WriteLine("e2=" & e2)
        End If

        ' Step 3: Convert to a decimal power base using 128-bit arithmetic.
        ' -1077 = 1 - 1023 - 53 - 2 <= e_2 - 2 <= 2046 - 1023 - 53 - 2 = 968
        Dim dv, dp, dm As Long
        'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
        'ORIGINAL LINE: final int e10;
        Dim dmIsTrailingZeros As Boolean = False, dvIsTrailingZeros As Boolean = False
        If e2 >= 0 Then
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int q = Math.max(0, ((e2 * 78913) >>> 18) - 1);
            Dim q As Integer = Math.Max(0, (CInt(CUInt((e2 * 78913)) >> 18)) - 1)
            ' k = constant + floor(log_2(5^q))
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int k = POW5_INV_BITCOUNT + pow5bits(q) - 1;
            Dim k As Integer = POW5_INV_BITCOUNT + pow5bits(q) - 1
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int i = -e2 + q + k;
            Dim i As Integer = -e2 + q + k
            dv = mulPow5InvDivPow2(mv, q, i)
            dp = mulPow5InvDivPow2(mp, q, i)
            dm = mulPow5InvDivPow2(mm, q, i)
            e10 = q
            If DEBUG Then
                Console.WriteLine(mv & " * 2^" & e2)
                Console.WriteLine("V+=" & dp)
                Console.WriteLine("V =" & dv)
                Console.WriteLine("V-=" & dm)
            End If
            If DEBUG Then
                Dim exact As Long = POW5_INV(q) * New BigInteger(mv) >> (-e2 + q + k)
                Console.WriteLine(exact & " " & POW5_INV(q).GetBitCount())
                If dv <> exact Then
                    Throw New InvalidOperationException()
                End If
            End If

            If q <= 21 Then
                If mv Mod 5 = 0 Then
                    dvIsTrailingZeros = multipleOfPowerOf5(mv, q)
                ElseIf roundingMode.acceptUpperBound(even) Then
                    dmIsTrailingZeros = multipleOfPowerOf5(mm, q)
                ElseIf multipleOfPowerOf5(mp, q) Then
                    dp -= 1
                End If
            End If
        Else
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int q = Math.max(0, ((-e2 * 732923) >>> 20) - 1);
            Dim q As Integer = Math.Max(0, (CInt(CUInt((-e2 * 732923)) >> 20)) - 1)
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int i = -e2 - q;
            Dim i As Integer = -e2 - q
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int k = pow5bits(i) - POW5_BITCOUNT;
            Dim k As Integer = pow5bits(i) - POW5_BITCOUNT
            'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
            'ORIGINAL LINE: final int j = q - k;
            Dim j As Integer = q - k
            dv = mulPow5divPow2(mv, i, j)
            dp = mulPow5divPow2(mp, i, j)
            dm = mulPow5divPow2(mm, i, j)
            e10 = q + e2
            If DEBUG Then
                Console.WriteLine(mv & " * 5^" & (-e2) & " / 10^" & q)
            End If
            If q <= 1 Then
                dvIsTrailingZeros = True
                If roundingMode.acceptUpperBound(even) Then
                    dmIsTrailingZeros = mmShift = 1
                Else
                    dp -= 1
                End If
            ElseIf q < 63 Then
                dvIsTrailingZeros = (mv And ((1L << (q - 1)) - 1)) = 0
            End If
        End If
        If DEBUG Then
            Console.WriteLine("d+=" & dp)
            Console.WriteLine("d =" & dv)
            Console.WriteLine("d-=" & dm)
            Console.WriteLine("e10=" & e10)
            Console.WriteLine("d-10=" & dmIsTrailingZeros)
            Console.WriteLine("d   =" & dvIsTrailingZeros)
            Console.WriteLine("Accept upper=" & roundingMode.acceptUpperBound(even))
            Console.WriteLine("Accept lower=" & roundingMode.acceptLowerBound(even))
        End If

        ' Step 4: Find the shortest decimal representation in the interval of legal representations.
        '
        ' We do some extra work here in order to follow Float/Double.toString semantics. In particular,
        ' that requires printing in scientific format if and only if the exponent is between -3 and 7,
        ' and it requires printing at least two decimal digits.
        '
        ' Above, we moved the decimal dot all the way to the right, so now we need to count digits to
        ' figure out the correct exponent for scientific notation.
        'JAVA TO VB CONVERTER WARNING: The original Java variable was marked 'final':
        'ORIGINAL LINE: final int vplength = decimalLength(dp);
        Dim vplength As Integer = decimalLength(dp)
        Dim exp As Integer = e10 + vplength - 1

        ' Double.toString semantics requires using scientific notation if and only if outside this range.
        Dim scientificNotation As Boolean = Not ((exp >= -3) AndAlso (exp < 7))

        Dim removed As Integer = 0

        Dim lastRemovedDigit As Integer = 0
        Dim output As Long
        If dmIsTrailingZeros OrElse dvIsTrailingZeros Then
            Do While dp \ 10 > dm \ 10
                If (dp < 100) AndAlso scientificNotation Then
                    ' Double.toString semantics requires printing at least two digits.
                    Exit Do
                End If
                dmIsTrailingZeros = dmIsTrailingZeros And dm Mod 10 = 0
                dvIsTrailingZeros = dvIsTrailingZeros And lastRemovedDigit = 0
                lastRemovedDigit = CInt(dv Mod 10)
                dp \= 10
                dv \= 10
                dm \= 10
                removed += 1
            Loop
            If dmIsTrailingZeros AndAlso roundingMode.acceptLowerBound(even) Then
                Do While dm Mod 10 = 0
                    If (dp < 100) AndAlso scientificNotation Then
                        ' Double.toString semantics requires printing at least two digits.
                        Exit Do
                    End If
                    dvIsTrailingZeros = dvIsTrailingZeros And lastRemovedDigit = 0
                    lastRemovedDigit = CInt(dv Mod 10)
                    dp \= 10
                    dv \= 10
                    dm \= 10
                    removed += 1
                Loop
            End If
            If dvIsTrailingZeros AndAlso (lastRemovedDigit = 5) AndAlso (dv Mod 2 = 0) Then
                ' Round even if the exact numbers is .....50..0.
                lastRemovedDigit = 4
            End If
            output = dv + (If((dv = dm AndAlso Not (dmIsTrailingZeros AndAlso roundingMode.acceptLowerBound(even))) OrElse (lastRemovedDigit >= 5), 1, 0))
        Else
            Do While dp \ 10 > dm \ 10
                If (dp < 100) AndAlso scientificNotation Then
                    ' Double.toString semantics requires printing at least two digits.
                    Exit Do
                End If
                lastRemovedDigit = CInt(dv Mod 10)
                dp \= 10
                dv \= 10
                dm \= 10
                removed += 1
            Loop
            output = dv + (If(dv = dm OrElse (lastRemovedDigit >= 5), 1, 0))
        End If
        Dim olength As Integer = vplength - removed

        If DEBUG Then
            Console.WriteLine("LAST_REMOVED_DIGIT=" & lastRemovedDigit)
            Console.WriteLine("VP=" & dp)
            Console.WriteLine("VR=" & dv)
            Console.WriteLine("VM=" & dm)
            Console.WriteLine("O=" & output)
            Console.WriteLine("OLEN=" & olength)
            Console.WriteLine("EXP=" & exp)
        End If

        ' Step 5: Print the decimal representation.
        ' We follow Double.toString semantics here.
        Dim result(23) As Char
        Dim index As Integer = 0
        If sign Then
            'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
        End If

        ' Values in the interval [1E-3, 1E7) are special.
        If scientificNotation Then
            ' Print in the format x.xxxxxE-yy.
            Dim i As Integer = 0
            Do While i < olength - 1
                Dim c As Integer = CInt(output Mod 10)
                output \= 10
                result(index + olength - i) = ChrW(AscW("0"c) + c)
                i += 1
            Loop
            result(index) = ChrW(AscW("0"c) + output Mod 10)
            result(index + 1) = "."c
            index += olength + 1
            If olength = 1 Then
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "0"c;
                result(index) = "0"c
                index += 1
            End If

            ' Print 'E', the exponent sign, and the exponent, which has at most three digits.
            'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "E"c;
            result(index) = "E"c
            index += 1
            If exp < 0 Then
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "-"c;
                result(index) = "-"c
                index += 1
                exp = -exp
            End If
            If exp >= 100 Then
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = (char)("0"c + exp / 100);
                result(index) = ChrW(AscW("0"c) + exp \ 100)
                index += 1
                exp = exp Mod 100
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = (char)("0"c + exp / 10);
                result(index) = ChrW(AscW("0"c) + exp \ 10)
                index += 1
            ElseIf exp >= 10 Then
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = (char)("0"c + exp / 10);
                result(index) = ChrW(AscW("0"c) + exp \ 10)
                index += 1
            End If
            'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = (char)("0"c + exp % 10);
            result(index) = ChrW(AscW("0"c) + exp Mod 10)
            index += 1
            Return New String(result, 0, index)
        Else
            ' Otherwise follow the Java spec for values in the interval [1E-3, 1E7).
            If exp < 0 Then
                ' Decimal dot is before any of the digits.
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "0"c;
                result(index) = "0"c
                index += 1
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "."c;
                result(index) = "."c
                index += 1
                For i As Integer = -1 To exp + 1 Step -1
                    'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                    'ORIGINAL LINE: result[index++] = "0"c;
                    result(index) = "0"c
                    index += 1
                Next i
                Dim current As Integer = index
                For i As Integer = 0 To olength - 1
                    result(current + olength - i - 1) = ChrW(AscW("0"c) + output Mod 10)
                    output \= 10
                    index += 1
                Next i
            ElseIf exp + 1 >= olength Then
                ' Decimal dot is after any of the digits.
                For i As Integer = 0 To olength - 1
                    result(index + olength - i - 1) = ChrW(AscW("0"c) + output Mod 10)
                    output \= 10
                Next i
                index += olength
                Dim i2 As Integer = olength
                Do While i2 < exp + 1
                    'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                    'ORIGINAL LINE: result[index++] = "0"c;
                    result(index) = "0"c
                    index += 1
                    i2 += 1
                Loop
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "."c;
                result(index) = "."c
                index += 1
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = "0"c;
                result(index) = "0"c
                index += 1
            Else
                ' Decimal dot is somewhere between the digits.
                Dim current As Integer = index + 1
                For i As Integer = 0 To olength - 1
                    If olength - i - 1 = exp Then
                        result(current + olength - i - 1) = "."c
                        current -= 1
                    End If
                    result(current + olength - i - 1) = ChrW(AscW("0"c) + output Mod 10)
                    output \= 10
                Next i
                index += olength + 1
            End If
            Return New String(result, 0, index)
        End If
    End Function

    Private Shared Function pow5bits(ByVal e As Integer) As Integer
        Return (CInt(CUInt((e * 1217359)) >> 19)) + 1
    End Function

    Private Shared Function decimalLength(ByVal v As Long) As Integer
        If v >= 1000000000000000000L Then
            Return 19
        End If
        If v >= 100000000000000000L Then
            Return 18
        End If
        If v >= 10000000000000000L Then
            Return 17
        End If
        If v >= 1000000000000000L Then
            Return 16
        End If
        If v >= 100000000000000L Then
            Return 15
        End If
        If v >= 10000000000000L Then
            Return 14
        End If
        If v >= 1000000000000L Then
            Return 13
        End If
        If v >= 100000000000L Then
            Return 12
        End If
        If v >= 10000000000L Then
            Return 11
        End If
        If v >= 1000000000L Then
            Return 10
        End If
        If v >= 100000000L Then
            Return 9
        End If
        If v >= 10000000L Then
            Return 8
        End If
        If v >= 1000000L Then
            Return 7
        End If
        If v >= 100000L Then
            Return 6
        End If
        If v >= 10000L Then
            Return 5
        End If
        If v >= 1000L Then
            Return 4
        End If
        If v >= 100L Then
            Return 3
        End If
        If v >= 10L Then
            Return 2
        End If
        Return 1
    End Function

    Private Shared Function multipleOfPowerOf5(ByVal value As Long, ByVal q As Integer) As Boolean
        Return pow5Factor(value) >= q
    End Function

    Private Shared Function pow5Factor(ByVal value As Long) As Integer
        ' We want to find the largest power of 5 that divides value.
        If (value Mod 5) <> 0 Then
            Return 0
        End If
        If (value Mod 25) <> 0 Then
            Return 1
        End If
        If (value Mod 125) <> 0 Then
            Return 2
        End If
        If (value Mod 625) <> 0 Then
            Return 3
        End If
        Dim count As Integer = 4
        value \= 625
        Do While value > 0
            If value Mod 5 <> 0 Then
                Return count
            End If
            value \= 5
            count += 1
        Loop
        Throw New ArgumentException("" & value)
    End Function

    ''' <summary>
    ''' Compute the high digits of m * 5^p / 10^q = m * 5^(p - q) / 2^q = m * 5^i / 2^j, with q chosen
    ''' such that m * 5^i / 2^j has sufficiently many decimal digits to represent the original floating
    ''' point number.
    ''' </summary>
    Private Shared Function mulPow5divPow2(ByVal m As Long, ByVal i As Integer, ByVal j As Integer) As Long
        ' m has at most 55 bits.
        Dim mHigh As Long = CLng(CULng(m) >> 31)
        Dim mLow As Long = m And &H7FFFFFFF
        Dim bits13 As Long = mHigh * POW5_SPLIT(i)(0) ' 124
        Dim bits03 As Long = mLow * POW5_SPLIT(i)(0) ' 93
        Dim bits12 As Long = mHigh * POW5_SPLIT(i)(1) ' 93
        Dim bits02 As Long = mLow * POW5_SPLIT(i)(1) ' 62
        Dim bits11 As Long = mHigh * POW5_SPLIT(i)(2) ' 62
        Dim bits01 As Long = mLow * POW5_SPLIT(i)(2) ' 31
        Dim bits10 As Long = mHigh * POW5_SPLIT(i)(3) ' 31
        Dim bits00 As Long = mLow * POW5_SPLIT(i)(3) ' 0
        Dim actualShift As Integer = j - 3 * 31 - 21
        If actualShift < 0 Then
            Throw New ArgumentException("" & actualShift)
        End If
        Return CLng(CULng(((CLng(CULng(((CLng(CULng(((CLng(CULng(((CLng(CULng(bits00) >> 31)) + bits01 + bits10)) >> 31)) + bits02 + bits11)) >> 31)) + bits03 + bits12)) >> 21)) + (bits13 << 10))) >> actualShift)
    End Function

    ''' <summary>
    ''' Compute the high digits of m / 5^i / 2^j such that the result is accurate to at least 9
    ''' decimal digits. i and j are already chosen appropriately.
    ''' </summary>
    Private Shared Function mulPow5InvDivPow2(ByVal m As Long, ByVal i As Integer, ByVal j As Integer) As Long
        ' m has at most 55 bits.
        Dim mHigh As Long = CLng(CULng(m) >> 31)
        Dim mLow As Long = m And &H7FFFFFFF
        Dim bits13 As Long = mHigh * POW5_INV_SPLIT(i)(0)
        Dim bits03 As Long = mLow * POW5_INV_SPLIT(i)(0)
        Dim bits12 As Long = mHigh * POW5_INV_SPLIT(i)(1)
        Dim bits02 As Long = mLow * POW5_INV_SPLIT(i)(1)
        Dim bits11 As Long = mHigh * POW5_INV_SPLIT(i)(2)
        Dim bits01 As Long = mLow * POW5_INV_SPLIT(i)(2)
        Dim bits10 As Long = mHigh * POW5_INV_SPLIT(i)(3)
        Dim bits00 As Long = mLow * POW5_INV_SPLIT(i)(3)

        Dim actualShift As Integer = j - 3 * 31 - 21
        If actualShift < 0 Then
            Throw New ArgumentException("" & actualShift)
        End If
        Return CLng(CULng(((CLng(CULng(((CLng(CULng(((CLng(CULng(((CLng(CULng(bits00) >> 31)) + bits01 + bits10)) >> 31)) + bits02 + bits11)) >> 31)) + bits03 + bits12)) >> 21)) + (bits13 << 10))) >> actualShift)
    End Function
End Class
