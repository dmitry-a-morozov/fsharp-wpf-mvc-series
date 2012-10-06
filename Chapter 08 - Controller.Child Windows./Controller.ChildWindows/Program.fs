
open System
open System.Windows
open Mvc.Wpf.Sample
open Mvc.Wpf

[<STAThread>] 
do 
    SimpleController(view = SampleView()) |> Controller.start |> ignore
