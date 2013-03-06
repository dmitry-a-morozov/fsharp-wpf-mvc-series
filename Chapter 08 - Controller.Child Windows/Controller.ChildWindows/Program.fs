
open System
open FSharp.Windows.Sample
open FSharp.Windows

[<STAThread>] 
do 
    (SampleView(), SampleController()) |> Mvc.start |> ignore
