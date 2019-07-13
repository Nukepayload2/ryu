Module D2sIntrinsics
    Function umul128(a As ULong, b As ULong, ByRef productHi As ULong) As ULong
        ' The casts here help MSVC to avoid calls to the __allmul library function.
        Dim aLo As UInteger = CUInt(a)
        Dim aHi As UInteger = CUInt(a >> 32)
        Dim bLo As UInteger = CUInt(b)
        Dim bHi As UInteger = CUInt(b >> 32)

        Dim b00 As ULong = CULng(aLo) * bLo
        Dim b01 As ULong = CULng(aLo) * bHi
        Dim b10 As ULong = CULng(aHi) * bLo
        Dim b11 As ULong = CULng(aHi) * bHi

        Dim b00Lo As UInteger = CUInt(b00)
        Dim b00Hi As UInteger = CUInt(b00 >> 32)

        Dim mid1 As ULong = b10 + b00Hi
        Dim mid1Lo As UInteger = CUInt(mid1)
        Dim mid1Hi As UInteger = CUInt(mid1 >> 32)

        Dim mid2 As ULong = b01 + mid1Lo
        Dim mid2Lo As UInteger = CUInt(mid2)
        Dim mid2Hi As UInteger = CUInt(mid2 >> 32)

        Dim pHi As ULong = b11 + mid1Hi + mid2Hi
        Dim pLo As ULong = (CULng(mid2Lo) << 32) Or b00Lo

        productHi = pHi
        Return pLo
    End Function

    Function shiftright128(lo As ULong, hi As ULong, dist As Integer) As ULong
        ' We don't need to handle the case dist >= 64 here (see above).
        Debug.Assert(dist < 64)

        ' Avoid a 64-bit shift by taking advantage of the range of shift values.
        Debug.Assert(dist >= 32)
        Return (hi << (64 - dist)) Or (CUInt(lo >> 32) >> (dist - 32))
    End Function

    Function div5(x As ULong) As ULong
        Return x \ 5UL
    End Function

    Function div10(x As ULong) As ULong
        Return x \ 10UL
    End Function

    Function div100(x As ULong) As ULong
        Return x \ 100UL
    End Function

    Function div1e8(x As ULong) As ULong
        Return x \ 100000000UL
    End Function

    Function div1e9(x As ULong) As ULong
        Return x \ 1000000000UL
    End Function

    Function mod1e9(x As ULong) As UInteger
        Return CUInt(x - 1000000000UL * div1e9(x))
    End Function

    Function pow5Factor(value As ULong) As UInteger
        Dim count As UInteger = 0
        Do
            Debug.Assert(value <> Nothing)
            Dim q As ULong = div5(value)
            Dim r As UInteger = (CUInt(value)) - 5UI * (CUInt(q))
            If r <> 0 Then
                Exit Do
            End If
            value = q
            count += 1UI
        Loop
        Return count
    End Function

    ' Returns true if value is divisible by 5^p.
    Function multipleOfPowerOf5(value As ULong, p As UInteger) As Boolean
        ' I tried a case distinction on p, but there was no performance difference.
        Return pow5Factor(value) >= p
    End Function

    ' Returns true if value is divisible by 2^p.
    Function multipleOfPowerOf2(value As ULong, p As Integer) As Boolean
        Debug.Assert(value <> Nothing)
        ' return __builtin_ctzll(value) >= p;
        Return (value And ((1UL << p) - 1UL)) = Nothing
    End Function

End Module
