module Mvc.Wpf.Sample.Test

open System 
open Xunit
open Swensen.Unquote.Assertions
open Mvc.Wpf

let controller = 
    SimpleController(view = {
        new IView<SampleEvents, SampleModel> with
            member __.Subscribe _ = raise <| NotImplementedException()
            member __.SetBindings _ = raise <| NotImplementedException()
    })

[<Fact>]
let InitModel() = 
    let model = SampleModel.Create()
    controller.InitModel model
    test <@ model.X = 0 @>
    test <@ model.Y = 0 @>
    test <@ model.Result = 0 @>

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
