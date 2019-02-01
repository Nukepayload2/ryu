Imports BenchmarkDotNet.Running

Module Program

    Sub Main(args() As String)
        BenchmarkRunner.Run(Reflection.Assembly.GetExecutingAssembly)
    End Sub
End Module
