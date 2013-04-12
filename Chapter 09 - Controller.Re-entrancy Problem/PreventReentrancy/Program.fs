
open System
open System.Windows
open FSharp.Windows.Sample
open FSharp.Windows

[<STAThread>] 
do 
    Mvc.startDialog(SampleView(), SampleController()) |> ignore
