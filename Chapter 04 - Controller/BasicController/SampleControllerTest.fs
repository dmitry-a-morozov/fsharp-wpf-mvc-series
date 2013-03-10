module FSharp.Windows.Sample.Test

open System 
open System.Diagnostics
open Xunit
open Swensen.Unquote.Assertions
open FSharp.Windows
open FSharp.Windows.Sample

let controller = SampleController()
let icontroller : IController<_, _> = upcast controller

[<Fact>]
let InitModel() = 
    let model = SampleModel.Create()
    icontroller.EventHandler Clear model
    model.X =? 0
    model.Y =? 0
    model.Result =? 0 
    model.Title =? ("Process name: " + Process.GetCurrentProcess().ProcessName)

[<Fact>]
let Add() = 
    let model : SampleModel = Model.Create()
    model.X <- 3
    model.Y <- 5
    icontroller.EventHandler Add model
    test <@ model.Result = 8 @>

[<Fact>]
let Subtract() = 
    let model = SampleModel.Create()
    controller.Subtract 11 9 model
    test <@ model.Result = 2 @>
