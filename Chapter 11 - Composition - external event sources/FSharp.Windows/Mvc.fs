namespace FSharp.Windows

open System
open System.Reflection
open System.Reactive
open System.ComponentModel

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>) =

    static let internalPreserveStackTrace = lazy typeof<Exception>.GetMethod("InternalPreserveStackTrace", BindingFlags.Instance ||| BindingFlags.NonPublic)
    let mutable onError = fun _ exn -> 
        internalPreserveStackTrace.Value.Invoke(exn, [||]) |> ignore
        raise exn |> ignore
    
    member this.Start() =
        controller.InitModel model
        view.SetBindings model

        Observer.Create (fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> this.OnError event exn
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = this.OnError event,
                    cancellationContinuation = ignore))

        |> Observer.notifyOnCurrentSynchronizationContext
        |> Observer.preventReentrancy
#if DEBUG
        |> Observer.Checked
#endif
        |> view.Subscribe

    member this.StartDialog() =
        use subscription = this.Start()
        view.ShowDialog()

    member this.StartWindow() =
        async {
            use subscription = this.Start()
            return! view.Show()
        }

    abstract OnError : ('Events -> exn -> unit) with get, set
    default this.OnError with get() = onError and set value = onError <- value

    member this.Compose(childController : IController<'EX, 'MX>, childView : IPartialView<'EX, 'MX>, childModelSelector : _ -> 'MX) = 
        let compositeView = {
                new IView<_, _> with
                    member __.Subscribe observer = (Observable.unify view childView).Subscribe(observer)
                    member __.SetBindings model =
                        view.SetBindings model  
                        model |> childModelSelector |> childView.SetBindings
                    member __.Show() = view.Show()
                    member __.ShowDialog() = view.ShowDialog()
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

    static member (<+>) (mvc : Mvc<_, _>,  (childController, childView, childModelSelector)) = 
        mvc.Compose(childController, childView, childModelSelector)

    member this.Compose(childController : IController<_, _>, events : IObservable<_>) = 
        let childView = {
            new IPartialView<_, _> with
                member __.Subscribe observer = events.Subscribe observer
                member __.SetBindings _ = () 
        }
        this.Compose(childController, childView, id)

        

    