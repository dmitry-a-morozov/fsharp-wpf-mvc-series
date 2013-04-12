namespace FSharp.Windows

open System.ComponentModel
open System
open System.Reflection

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>) =

    let mutable onError = fun _ (exn : exn) -> exn.Rethrow()

    member this.Activate() =
        controller.InitModel model
        view.SetBindings model

        Observer.create <| fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> this.OnError event exn
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = this.OnError event,
                    cancellationContinuation = ignore)

        |> Observer.notifyOnDispatcher 
        |> Observer.preventReentrancy 
        |> view.Subscribe 

    abstract OnError : ('Events -> exn -> unit) with get, set
    default this.OnError with get() = onError and set value = onError <- value

    member this.Compose(childController : IController<'EX, 'MX>, childView : IView<'EX, 'MX>, childModelSelector : _ -> 'MX) = 
        let compositeView = {
                new IView<_, _> with
                    member __.Subscribe observer = (Observable.unify view childView).Subscribe(observer)
                    member __.SetBindings model =
                        view.SetBindings model  
                        model |> childModelSelector |> childView.SetBindings
        }

        let compositeController = { 
            new IController<_, _> with
                member __.InitModel model = 
                    controller.InitModel model
                    model |> childModelSelector |> childController.InitModel
                member __.Dispatcher = function 
                    | Choice1Of2 e -> controller.Dispatcher e
                    | Choice2Of2 e -> 
                        match childController.Dispatcher e with
                        | Sync handler -> Sync(childModelSelector >> handler)  
                        | Async handler -> Async(childModelSelector >> handler) 
        }

        Mvc(model, compositeView, compositeController)

    static member (<+>) (mvc : Mvc<_, _>,  (childController : #IController<_, _>, childView, childModelSelector)) = 
        mvc.Compose(childController, childView, childModelSelector)

    member this.Compose(childController : IController<_, _>, events : IObservable<_>) = 
        let childView = {
            new IView<_, _> with
                member __.Subscribe observer = events.Subscribe observer
                member __.SetBindings _ = () 
        }
        this.Compose(childController, childView, id)

    static member StartDialog(model, dialog : #IDialog<_>, controller) = 
        use subscription = Mvc<'Events, 'Model>(model, dialog, controller).Activate()
        dialog.Show()

[<RequireQualifiedAccess>]
module Mvc = 

    let inline startDialog(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        if Mvc<'Events, ^Model>.StartDialog(model, view, controller) then Some model else None

