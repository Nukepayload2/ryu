Imports System.Numerics
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices

Module RectangularArrays
    '----------------------------------------------------------------------------------------
    '	Copyright © 2007 - 2019 Tangible Software Solutions, Inc.
    '	This module can be used by anyone provided that the copyright notice remains intact.
    '
    '	This module includes methods to convert Java rectangular arrays (jagged arrays
    '	with inner arrays of the same length).
    '----------------------------------------------------------------------------------------
    Public Function RectangularIntegerArray(ByVal size1 As Integer, ByVal size2 As Integer) As Integer()()
        Dim newArray As Integer()() = New Integer(size1 - 1)() {}
        For array1 As Integer = 0 To size1 - 1
            newArray(array1) = New Integer(size2 - 1) {}
        Next array1

        Return newArray
    End Function
End Module

Module BigIntegerExtensions
    <Extension>
    Function GetBitCount(this As BigInteger) As Integer
        Dim adjusted = this
        If this < 0 Then
            adjusted = -1
        Else
            adjusted += 1
        End If
        Return Log2Ceiling(adjusted)
    End Function

    Private Function Log2Ceiling(adjusted As BigInteger) As Integer
        Return BigInteger.Log(adjusted) / BigInteger.Log(2)
    End Function

    <Extension>
    Function GetPow5Bits(this As Integer) As Integer
        Return CInt((this.AsUInteger * 1217359) >> 19) + 1
    End Function
End Module

Module SingleAndIntegerBitConvert
    <StructLayout(LayoutKind.Explicit)>
    Private Structure SingleIntegerUnion
        <FieldOffset(0)>
        Dim SingleValue As Single
        <FieldOffset(0)>
        Dim IntegerValue As Integer
    End Structure

    <Extension>
    Public Function AsInteger(SingleValue As Single) As Integer
        Return (New SingleIntegerUnion With {.SingleValue = SingleValue}).IntegerValue
    End Function

    <Extension>
    Public Function AsSingle(IntegerValue As Integer) As Single
        Return (New SingleIntegerUnion With {.IntegerValue = IntegerValue}).SingleValue
    End Function
End Module

Public Module IntegerAndUIntegerBitConvert
    <StructLayout(LayoutKind.Explicit)>
    Private Structure IntegerUIntegerUnion
        <FieldOffset(0)>
        Dim IntegerValue As Integer
        <FieldOffset(0)>
        Dim UIntegerValue As UInteger
    End Structure

    <Extension>
    Public Function AsUInteger(IntegerValue As Integer) As UInteger
        Return (New IntegerUIntegerUnion With {.IntegerValue = IntegerValue}).UIntegerValue
    End Function

    <Extension>
    Public Function AsInteger(UIntegerValue As UInteger) As Integer
        Return (New IntegerUIntegerUnion With {.UIntegerValue = UIntegerValue}).IntegerValue
    End Function
End Module
