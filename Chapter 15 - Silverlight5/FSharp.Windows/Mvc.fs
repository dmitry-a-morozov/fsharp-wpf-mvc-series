namespace FSharp.Windows

open System.ComponentModel
open System
open System.Reflection

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>) =

    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe (fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> this.OnException(event, exn)
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = (fun exn -> this.OnException(event, exn)),
                    cancellationContinuation = ignore
                )
        )

    abstract OnException : 'Events * exn -> unit
    default this.OnException(_, exn) = exn.Rethrow()

[<RequireQualifiedAccess>]
module Mvc = 

    let inline start(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        Mvc<'Events, ^Model>(model, view, controller).Start()

