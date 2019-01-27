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
''' An implementation of Ryu for float.
''' </summary>
Public NotInheritable Class RyuFloat
    Private Shared DEBUG As Boolean = False

    Private Const FLOAT_MANTISSA_BITS As Integer = 23
    Private Shared ReadOnly FLOAT_MANTISSA_MASK As Integer = (1 << FLOAT_MANTISSA_BITS) - 1

    Private Const FLOAT_EXPONENT_BITS As Integer = 8
    Private Shared ReadOnly FLOAT_EXPONENT_MASK As Integer = (1 << FLOAT_EXPONENT_BITS) - 1
    Private Shared ReadOnly FLOAT_EXPONENT_BIAS As Integer = (1 << (FLOAT_EXPONENT_BITS - 1)) - 1

    Private Const LOG10_2_DENOMINATOR As Long = 10000000L
    Private Shared ReadOnly LOG10_2_NUMERATOR As Long = CLng(Math.Truncate(LOG10_2_DENOMINATOR * Math.Log10(2)))

    Private Const LOG10_5_DENOMINATOR As Long = 10000000L
    Private Shared ReadOnly LOG10_5_NUMERATOR As Long = CLng(Math.Truncate(LOG10_5_DENOMINATOR * Math.Log10(5)))

    Private Const LOG2_5_DENOMINATOR As Long = 10000000L
    'JAVA TO VB CONVERTER WARNING: Java to VB Converter cannot determine whether both operands of this division are integer types - if they are then you should use the VB integer division operator:
    Private Shared ReadOnly LOG2_5_NUMERATOR As Long = CLng(Math.Truncate(LOG2_5_DENOMINATOR * (Math.Log(5) / Math.Log(2))))

    Private Const POS_TABLE_SIZE As Integer = 47
    Private Const INV_TABLE_SIZE As Integer = 31

    ' Only for debugging.
    Private Shared ReadOnly POW5(POS_TABLE_SIZE - 1) As BigInteger
    Private Shared ReadOnly POW5_INV(INV_TABLE_SIZE - 1) As BigInteger

    Private Const POW5_BITCOUNT As Integer = 61
    Private Const POW5_HALF_BITCOUNT As Integer = 31
    'JAVA TO VB CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
    'ORIGINAL LINE: Private Shared ReadOnly POW5_SPLIT[][] As Integer = new Integer[POS_TABLE_SIZE][2]
    Private Shared ReadOnly POW5_SPLIT()() As Integer = RectangularIntegerArray(POS_TABLE_SIZE, 2)

    Private Const POW5_INV_BITCOUNT As Integer = 59
    Private Const POW5_INV_HALF_BITCOUNT As Integer = 31
    'JAVA TO VB CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
    'ORIGINAL LINE: Private Shared ReadOnly POW5_INV_SPLIT[][] As Integer = new Integer[INV_TABLE_SIZE][2]
    Private Shared ReadOnly POW5_INV_SPLIT()() As Integer = RectangularIntegerArray(INV_TABLE_SIZE, 2)

    Shared Sub New()
        Dim mask As BigInteger = New BigInteger(1) << (POW5_HALF_BITCOUNT - BigInteger.One)
        Dim maskInv As BigInteger = New BigInteger(1) << (POW5_INV_HALF_BITCOUNT - BigInteger.One)
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
                POW5_SPLIT(i)(0) = pow >> (pow5len - POW5_BITCOUNT + POW5_HALF_BITCOUNT)
                POW5_SPLIT(i)(1) = pow >> (pow5len - POW5_BITCOUNT) And mask
            End If

            If i < POW5_INV.Length Then
                Dim j As Integer = pow5len - 1 + POW5_INV_BITCOUNT
                Dim inv As BigInteger = ((BigInteger.One << j) / pow) + BigInteger.One
                POW5_INV(i) = inv
                POW5_INV_SPLIT(i)(0) = inv >> POW5_INV_HALF_BITCOUNT
                POW5_INV_SPLIT(i)(1) = inv And maskInv
            End If
        Next i
    End Sub

    Public Shared Function floatToString(ByVal value As Single) As String
        Return floatToString(value, RoundingMode.ROUND_EVEN)
    End Function

    Public Shared Function floatToString(ByVal value As Single, ByVal roundingMode As RoundingMode) As String
        ' Step 1: Decode the floating point number, and unify normalized and subnormal cases.
        ' First, handle all the trivial cases.
        If Single.IsNaN(value) Then
            Return "NaN"
        End If
        If value = Single.PositiveInfinity Then
            Return "Infinity"
        End If
        If value = Single.NegativeInfinity Then
            Return "-Infinity"
        End If
        Dim bits As Integer = value.AsInteger
        If bits = 0 Then
            Return "0.0"
        End If
        If bits = &H80000000L Then
            Return "-0.0"
        End If

        ' Otherwise extract the mantissa and exponent bits and run the full algorithm.
        Dim ieeeExponent As Integer = (bits >> FLOAT_MANTISSA_BITS) And FLOAT_EXPONENT_MASK
        Dim ieeeMantissa As Integer = bits And FLOAT_MANTISSA_MASK
        ' By default, the correct mantissa starts with a 1, except for denormal numbers.
        Dim e2 As Integer
        Dim m2 As Integer
        If ieeeExponent = 0 Then
            e2 = 1 - FLOAT_EXPONENT_BIAS - FLOAT_MANTISSA_BITS
            m2 = ieeeMantissa
        Else
            e2 = ieeeExponent - FLOAT_EXPONENT_BIAS - FLOAT_MANTISSA_BITS
            m2 = ieeeMantissa Or (1 << FLOAT_MANTISSA_BITS)
        End If

        Dim sign As Boolean = bits < 0
        If DEBUG Then
            Console.WriteLine("IN=" & bits.ToString("X"))
            Console.WriteLine("   S=" & (If(sign, "-", "+")) & " E=" & e2 & " M=" & m2)
        End If

        ' Step 2: Determine the interval of legal decimal representations.
        Dim even As Boolean = (m2 And 1) = 0
        Dim mv As Integer = 4 * m2
        Dim mp As Integer = 4 * m2 + 2
        Dim mm As Integer = 4 * m2 - (If((m2 <> (1L << FLOAT_MANTISSA_BITS)) OrElse (ieeeExponent <= 1), 2, 1))
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

            Console.WriteLine("Exact values")
            Console.WriteLine("  m =" & mv)
            Console.WriteLine("  E =" & e10)
            Console.WriteLine("  d+=" & sp)
            Console.WriteLine("  d =" & sv)
            Console.WriteLine("  d-=" & sm)
            Console.WriteLine("  e2=" & e2)
        End If

        ' Step 3: Convert to a decimal power base using 128-bit arithmetic.
        ' -151 = 1 - 127 - 23 - 2 <= e_2 - 2 <= 254 - 127 - 23 - 2 = 102
        Dim dp, dv, dm As Integer
        Dim dpIsTrailingZeros, dvIsTrailingZeros, dmIsTrailingZeros As Boolean
        Dim lastRemovedDigit As Integer = 0
        If e2 >= 0 Then
            ' Compute m * 2^e_2 / 10^q = m * 2^(e_2 - q) / 5^q
            Dim q As Integer = CInt(e2 * LOG10_2_NUMERATOR \ LOG10_2_DENOMINATOR)
            Dim k As Integer = POW5_INV_BITCOUNT + pow5bits(q) - 1
            Dim i As Integer = -e2 + q + k
            dv = CInt(mulPow5InvDivPow2(mv, q, i))
            dp = CInt(mulPow5InvDivPow2(mp, q, i))
            dm = CInt(mulPow5InvDivPow2(mm, q, i))
            If q <> 0 AndAlso ((dp - 1) \ 10 <= dm \ 10) Then
                ' We need to know one removed digit even if we are not going to loop below. We could use
                ' q = X - 1 above, except that would require 33 bits for the result, and we've found that
                ' 32-bit arithmetic is faster even on 64-bit machines.
                Dim l As Integer = POW5_INV_BITCOUNT + pow5bits(q - 1) - 1
                lastRemovedDigit = CInt(mulPow5InvDivPow2(mv, q - 1, -e2 + q - 1 + l) Mod 10)
            End If
            e10 = q
            If DEBUG Then
                Console.WriteLine(mv & " * 2^" & e2 & " / 10^" & q)
            End If

            dpIsTrailingZeros = pow5Factor(mp) >= q
            dvIsTrailingZeros = pow5Factor(mv) >= q
            dmIsTrailingZeros = pow5Factor(mm) >= q
        Else
            ' Compute m * 5^(-e_2) / 10^q = m * 5^(-e_2 - q) / 2^q
            Dim q As Integer = CInt(-e2 * LOG10_5_NUMERATOR \ LOG10_5_DENOMINATOR)
            Dim i As Integer = -e2 - q
            Dim k As Integer = pow5bits(i) - POW5_BITCOUNT
            Dim j As Integer = q - k
            dv = CInt(mulPow5divPow2(mv, i, j))
            dp = CInt(mulPow5divPow2(mp, i, j))
            dm = CInt(mulPow5divPow2(mm, i, j))
            If q <> 0 AndAlso ((dp - 1) \ 10 <= dm \ 10) Then
                j = q - 1 - (pow5bits(i + 1) - POW5_BITCOUNT)
                lastRemovedDigit = CInt(mulPow5divPow2(mv, i + 1, j) Mod 10)
            End If
            e10 = q + e2 ' Note: e2 and e10 are both negative here.
            If DEBUG Then
                Console.WriteLine(mv & " * 5^" & (-e2) & " / 10^" & q & " = " & mv & " * 5^" & (-e2 - q) & " / 2^" & q)
            End If

            dpIsTrailingZeros = 1 >= q
            dvIsTrailingZeros = (q < FLOAT_MANTISSA_BITS) AndAlso (mv And ((1 << (q - 1)) - 1)) = 0
            dmIsTrailingZeros = (If(mm Mod 2 = 1, 0, 1)) >= q
        End If
        If DEBUG Then
            Console.WriteLine("Actual values")
            Console.WriteLine("  d+=" & dp)
            Console.WriteLine("  d =" & dv)
            Console.WriteLine("  d-=" & dm)
            Console.WriteLine("  last removed=" & lastRemovedDigit)
            Console.WriteLine("  e10=" & e10)
            Console.WriteLine("  d+10=" & dpIsTrailingZeros)
            Console.WriteLine("  d   =" & dvIsTrailingZeros)
            Console.WriteLine("  d-10=" & dmIsTrailingZeros)
        End If

        ' Step 4: Find the shortest decimal representation in the interval of legal representations.
        '
        ' We do some extra work here in order to follow Float/Double.toString semantics. In particular,
        ' that requires printing in scientific format if and only if the exponent is between -3 and 7,
        ' and it requires printing at least two decimal digits.
        '
        ' Above, we moved the decimal dot all the way to the right, so now we need to count digits to
        ' figure out the correct exponent for scientific notation.
        Dim dplength As Integer = decimalLength(dp)
        Dim exp As Integer = e10 + dplength - 1

        ' Float.toString semantics requires using scientific notation if and only if outside this range.
        Dim scientificNotation As Boolean = Not ((exp >= -3) AndAlso (exp < 7))

        Dim removed As Integer = 0
        If dpIsTrailingZeros AndAlso Not roundingMode.acceptUpperBound(even) Then
            dp -= 1
        End If

        Do While dp \ 10 > dm \ 10
            If (dp < 100) AndAlso scientificNotation Then
                ' We print at least two digits, so we might as well stop now.
                Exit Do
            End If
            dmIsTrailingZeros = dmIsTrailingZeros And dm Mod 10 = 0
            dp \= 10
            lastRemovedDigit = dv Mod 10
            dv \= 10
            dm \= 10
            removed += 1
        Loop
        If dmIsTrailingZeros AndAlso roundingMode.acceptLowerBound(even) Then
            Do While dm Mod 10 = 0
                If (dp < 100) AndAlso scientificNotation Then
                    ' We print at least two digits, so we might as well stop now.
                    Exit Do
                End If
                dp \= 10
                lastRemovedDigit = dv Mod 10
                dv \= 10
                dm \= 10
                removed += 1
            Loop
        End If

        If dvIsTrailingZeros AndAlso (lastRemovedDigit = 5) AndAlso (dv Mod 2 = 0) Then
            ' Round down not up if the number ends in X50000 and the number is even.
            lastRemovedDigit = 4
        End If
        Dim output As Integer = dv + (If((dv = dm AndAlso Not (dmIsTrailingZeros AndAlso roundingMode.acceptLowerBound(even))) OrElse (lastRemovedDigit >= 5), 1, 0))
        Dim olength As Integer = dplength - removed

        If DEBUG Then
            Console.WriteLine("Actual values after loop")
            Console.WriteLine("  d+=" & dp)
            Console.WriteLine("  d =" & dv)
            Console.WriteLine("  d-=" & dm)
            Console.WriteLine("  last removed=" & lastRemovedDigit)
            Console.WriteLine("  e10=" & e10)
            Console.WriteLine("  d+10=" & dpIsTrailingZeros)
            Console.WriteLine("  d-10=" & dmIsTrailingZeros)
            Console.WriteLine("  output=" & output)
            Console.WriteLine("  output_length=" & olength)
            Console.WriteLine("  output_exponent=" & exp)
        End If

        ' Step 5: Print the decimal representation.
        ' We follow Float.toString semantics here.
        Dim result(14) As Char
        Dim index As Integer = 0
        If sign Then
            'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = "-"c;
            result(index) = "-"c
            index += 1
        End If

        If scientificNotation Then
            ' Print in the format x.xxxxxE-yy.
            Dim i As Integer = 0
            Do While i < olength - 1
                Dim c As Integer = output Mod 10
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

            ' Print 'E', the exponent sign, and the exponent, which has at most two digits.
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
            If exp >= 10 Then
                'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
                'ORIGINAL LINE: result[index++] = (char)("0"c + exp / 10);
                result(index) = ChrW(AscW("0"c) + exp \ 10)
                index += 1
            End If
            'JAVA TO VB CONVERTER WARNING: An assignment within expression was extracted from the following statement:
            'ORIGINAL LINE: result[index++] = (char)("0"c + exp % 10);
            result(index) = ChrW(AscW("0"c) + exp Mod 10)
            index += 1
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
                For i = 0 To olength - 1
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
        End If
        Return New String(result, 0, index)
    End Function

    Private Shared Function pow5bits(ByVal e As Integer) As Integer
        Return If(e = 0, 1, CInt((e * LOG2_5_NUMERATOR + LOG2_5_DENOMINATOR - 1) \ LOG2_5_DENOMINATOR))
    End Function

    ''' <summary>
    ''' Returns the exponent of the largest power of 5 that divides the given value, i.e., returns
    ''' i such that value = 5^i * x, where x is an integer.
    ''' </summary>
    Private Shared Function pow5Factor(ByVal value As Integer) As Integer
        Dim count As Integer = 0
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
    ''' Compute the exact result of [m * 5^(-e_2) / 10^q] = [m * 5^(-e_2 - q) / 2^q]
    ''' = [m * [5^(p - q)/2^k] / 2^(q - k)] = [m * POW5[i] / 2^j].
    ''' </summary>
    Private Shared Function mulPow5divPow2(ByVal m As Integer, ByVal i As Integer, ByVal j As Integer) As Long
        If j - POW5_HALF_BITCOUNT < 0 Then
            Throw New ArgumentException()
        End If
        Dim bits0 As Long = m * CLng(POW5_SPLIT(i)(0))
        Dim bits1 As Long = m * CLng(POW5_SPLIT(i)(1))
        Return (bits0 + (bits1 >> POW5_HALF_BITCOUNT)) >> (j - POW5_HALF_BITCOUNT)
    End Function

    ''' <summary>
    ''' Compute the exact result of [m * 2^p / 10^q] = [m * 2^(p - q) / 5 ^ q]
    ''' = [m * [2^k / 5^q] / 2^-(p - q - k)] = [m * POW5_INV[q] / 2^j].
    ''' </summary>
    Private Shared Function mulPow5InvDivPow2(ByVal m As Integer, ByVal q As Integer, ByVal j As Integer) As Long
        If j - POW5_INV_HALF_BITCOUNT < 0 Then
            Throw New ArgumentException()
        End If
        Dim bits0 As Long = m * CLng(POW5_INV_SPLIT(q)(0))
        Dim bits1 As Long = m * CLng(POW5_INV_SPLIT(q)(1))
        Return (bits0 + (bits1 >> POW5_INV_HALF_BITCOUNT)) >> (j - POW5_INV_HALF_BITCOUNT)
    End Function

    Private Shared Function decimalLength(ByVal v As Integer) As Integer
        Dim length As Integer = 10
        Dim factor As Integer = 1000000000
        Do While length > 0
            If v >= factor Then
                Exit Do
            End If
            factor \= 10
            length -= 1
        Loop
        Return length
    End Function
End Class
