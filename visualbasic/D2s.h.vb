Partial Module DoubleToString ' D2s_h
    Private Const DOUBLE_MANTISSA_BITS As Integer = 52
    Private Const DOUBLE_EXPONENT_BITS As Integer = 11
    Private Const DOUBLE_BIAS As Integer = 1023

    Private Const DOUBLE_POW5_INV_BITCOUNT As Integer = 122
    Private Const DOUBLE_POW5_BITCOUNT As Integer = 121

#If RYU_OPTIMIZE_SIZE Then

Private Const POW5_TABLE_SIZE As Integer = 26
Private Const DOUBLE_POW5_TABLE As uint64_t

Private Const DOUBLE_POW5_SPLIT2 As uint64_t
' Unfortunately, the results are sometimes off by one. We use an additional
' lookup table to store those cases and adjust the result.
Private Const POW5_OFFSETS As uint32_t


Private Const DOUBLE_POW5_INV_SPLIT2 As uint64_t
Private Const POW5_INV_OFFSETS As uint32_t

#If HAS_UINT128 Then

' Computes 5^i in the form required by Ryu, and stores it in the given pointer.
Private inline Sub double_computePow5(const i As uint32_t, uint64_t* result As const)
  Const MyBase As uint32_t = i / POW5_TABLE_SIZE
  Const base2 As uint32_t = MyBase * POW5_TABLE_SIZE
  Const offset As uint32_t = i - base2
  const uint64_t* const mul = DOUBLE_POW5_SPLIT2(MyBase)
  If offset = 0 Then
	result(0) = mul(0)
	result(1) = mul(1)
	Return
  End If
  Const m As uint64_t = DOUBLE_POW5_TABLE(offset)
  Const b0 As uint128_t = (CType(m, uint128_t)) * mul(0)
  Const b2 As uint128_t = (CType(m, uint128_t)) * mul(1)
  Const delta As uint32_t = pow5bits(i) - pow5bits(base2)
  Const shiftedSum As uint128_t = (b0 >> delta) + (b2 << (64 - delta)) + ((POW5_OFFSETS(MyBase) >> offset) And 1)
  result(0) = CType(shiftedSum, uint64_t)
  result(1) = CType(shiftedSum >> 64, uint64_t)
End Sub

' Computes 5^-i in the form required by Ryu, and stores it in the given pointer.
Private inline Sub double_computeInvPow5(const i As uint32_t, uint64_t* result As const)
  Const MyBase As uint32_t = (i + POW5_TABLE_SIZE - 1) / POW5_TABLE_SIZE
  Const base2 As uint32_t = MyBase * POW5_TABLE_SIZE
  Const offset As uint32_t = base2 - i
  const uint64_t* const mul = DOUBLE_POW5_INV_SPLIT2(MyBase) ' 1/5^base2
  If offset = 0 Then
	result(0) = mul(0)
	result(1) = mul(1)
	Return
  End If
  Const m As uint64_t = DOUBLE_POW5_TABLE(offset) ' 5^offset
  Const b0 As uint128_t = (CType(m, uint128_t)) * (mul(0) - 1)
  Const b2 As uint128_t = (CType(m, uint128_t)) * mul(1) ' 1/5^base2 * 5^offset = 1/5^(base2-offset) = 1/5^i
  Const delta As uint32_t = pow5bits(base2) - pow5bits(i)
  Const shiftedSum As uint128_t = ((b0 >> delta) + (b2 << (64 - delta))) + 1 + ((POW5_INV_OFFSETS(i / 16) >> ((i Mod 16) << 1)) And 3)
  result(0) = CType(shiftedSum, uint64_t)
  result(1) = CType(shiftedSum >> 64, uint64_t)
End Sub

#Else ' defined(HAS_UINT128)

' Computes 5^i in the form required by Ryu, and stores it in the given pointer.
Private inline Sub double_computePow5(const i As uint32_t, uint64_t* result As const)
  Const MyBase As uint32_t = i / POW5_TABLE_SIZE
  Const base2 As uint32_t = MyBase * POW5_TABLE_SIZE
  Const offset As uint32_t = i - base2
  const uint64_t* const mul = DOUBLE_POW5_SPLIT2(MyBase)
  If offset = 0 Then
	result(0) = mul(0)
	result(1) = mul(1)
	Return
  End If
  Const m As uint64_t = DOUBLE_POW5_TABLE(offset)
  Dim high1 As uint64_t
  Const low1 As uint64_t = umul128(m, mul(1), &high1)
  Dim high0 As uint64_t
  Const low0 As uint64_t = umul128(m, mul(0), &high0)
  Const sum As uint64_t = high0 + low1
  If sum < high0 Then
	high1 += 1 ' overflow into high1
  End If
  ' high1 | sum | low0
  Const delta As uint32_t = pow5bits(i) - pow5bits(base2)
  result(0) = shiftright128(low0, sum, delta) + ((POW5_OFFSETS(MyBase) >> offset) And 1)
  result(1) = shiftright128(sum, high1, delta)
End Sub

' Computes 5^-i in the form required by Ryu, and stores it in the given pointer.
Private inline Sub double_computeInvPow5(const i As uint32_t, uint64_t* result As const)
  Const MyBase As uint32_t = (i + POW5_TABLE_SIZE - 1) / POW5_TABLE_SIZE
  Const base2 As uint32_t = MyBase * POW5_TABLE_SIZE
  Const offset As uint32_t = base2 - i
  const uint64_t* const mul = DOUBLE_POW5_INV_SPLIT2(MyBase) ' 1/5^base2
  If offset = 0 Then
	result(0) = mul(0)
	result(1) = mul(1)
	Return
  End If
  Const m As uint64_t = DOUBLE_POW5_TABLE(offset)
  Dim high1 As uint64_t
  Const low1 As uint64_t = umul128(m, mul(1), &high1)
  Dim high0 As uint64_t
  Const low0 As uint64_t = umul128(m, mul(0) - 1, &high0)
  Const sum As uint64_t = high0 + low1
  If sum < high0 Then
	high1 += 1 ' overflow into high1
  End If
  ' high1 | sum | low0
  Const delta As uint32_t = pow5bits(base2) - pow5bits(i)
  result(0) = shiftright128(low0, sum, delta) + 1 + ((POW5_INV_OFFSETS(i / 16) >> ((i Mod 16) << 1)) And 3)
  result(1) = shiftright128(sum, high1, delta)
End Sub

#End If ' defined(HAS_UINT128)

#End If ' defined(RYU_OPTIMIZE_SIZE)

End Module
