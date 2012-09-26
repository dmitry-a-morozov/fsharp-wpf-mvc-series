module Mvc.Wpf.Sample.Test

open System 
open System.Diagnostics
open Xunit
open Swensen.Unquote.Assertions
open Mvc.Wpf

let controller = 
    SimpleController(view = {
        new IView<SampleEvents> with
            member __.Subscribe _ = raise <| NotImplementedException()
            member __.SetBindings _ = raise <| NotImplementedException()
    })

[<Fact>]
let InitModel() = 
    let model = SampleModel.Create()
    controller.InitModel model
    model.X =? 0
    model.Y =? 0
    model.Result =? 0 
    model.Title =? ("Process name: " + Process.GetCurrentProcess().ProcessName)

[<Fact>]
let Add() = 
    let model : SampleModel = Model.Create()
    model.X <- 3
    model.Y <- 5
    controller.EventHandler Add model
    test <@ model.Result = 8 @>

[<Fact>]
let Subtract() = 
    let model = SampleModel.Create()
    controller.Subtract 11 9 model
    test <@ model.Result = 2 @>
