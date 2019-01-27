Imports System.Collections.Generic
Imports System.Runtime.CompilerServices

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

Public Enum RoundingMode
    CONSERVATIVE
    ROUND_EVEN
End Enum

Module RoundingModeExtensions
    <Extension>
    Public Function acceptUpperBound(this As RoundingMode, even As Boolean) As Boolean
        Select Case this
            Case RoundingMode.CONSERVATIVE
                Return False
            Case RoundingMode.ROUND_EVEN
                Return even
        End Select
        Return False
    End Function

    <Extension>
    Public Function acceptLowerBound(this As RoundingMode, even As Boolean) As Boolean
        Select Case this
            Case RoundingMode.CONSERVATIVE
                Return False
            Case RoundingMode.ROUND_EVEN
                Return even
        End Select
        Return False
    End Function
End Module
