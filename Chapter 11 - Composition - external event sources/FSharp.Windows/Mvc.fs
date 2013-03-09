namespace FSharp.Windows

open System
open System.Reflection
open System.Reactive.Linq
open System.Reactive.Concurrency
open System.Reactive
open System.Threading
open System.ComponentModel

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>) =

    static let defaultReraise =  
        let internalPreserveStackTrace = lazy typeof<Exception>.GetMethod("InternalPreserveStackTrace", BindingFlags.Instance ||| BindingFlags.NonPublic)
        fun exn ->
            internalPreserveStackTrace.Value.Invoke(exn, [||]) |> ignore
            raise exn |> ignore
    
    member this.Activate() =
        controller.InitModel model
        view.SetBindings model

        let observer = Observer.Create(fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> this.OnException(event, exn)
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = (fun exn -> this.OnException(event, exn)),
                    cancellationContinuation = ignore))
#if DEBUG
        let observer = observer.Checked()
#endif
        view
            .ObserveOn(
                scheduler = SynchronizationContextScheduler(SynchronizationContext.Current, alwaysPost = false))
            .Subscribe(
                observer = Observer.Synchronize(observer, preventReentrancy = true))

    member this.Start() =
        use subscription = this.Activate()
        view.ShowDialog()

    member this.AsyncStart() =
        async {
            use subscription = this.Activate()
            return! view.Show()
        }

    abstract OnException : 'Events * exn -> unit
    default this.OnException(_, exn) = defaultReraise exn 

    member this.Compose(childController : IController<'EX, 'MX>, childView : IPartialView<'EX, 'MX>, childModelSelector : _ -> 'MX) = 
        let compositeView = {
                new IView<_, _> with
                    member __.Subscribe observer = (Observable.unify view childView).Subscribe(observer)
                    member __.SetBindings model =
                        view.SetBindings model  
                        model |> childModelSelector |> childView.SetBindings
                    member __.Show() = view.Show()
                    member __.ShowDialog() = view.ShowDialog()
                    member __.Close ok = view.Close ok
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
            new IPartialView<_, _> with
                member __.Subscribe observer = events.Subscribe observer
                member __.SetBindings _ = () 
        }
        this.Compose(childController, childView, id)

        

    