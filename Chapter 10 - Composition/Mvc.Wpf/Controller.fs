namespace Mvc.Wpf

open System
open System.Reactive.Linq
open System.Reactive.Concurrency
open System.Reactive
open System.Threading
open System.ComponentModel

type EventHandler<'M> = 
    | Sync of ('M -> unit)
    | Async of ('M -> Async<unit>)

[<AbstractClass>]
type Controller<'Event, 'Model when 'Model :> INotifyPropertyChanged>() =
    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

exception PreserveStackTraceWrapper of exn

[<AbstractClass>]
type SupervisingController<'Event, 'Model when 'Model :> INotifyPropertyChanged>(view : IView<'Event, 'Model>) =
    inherit Controller<'Event, 'Model>()

    member this.Activate model =
        this.InitModel model
        view.SetBindings model

        let observer = Observer.Create(fun e -> 
            match this.Dispatcher e with
            | Sync handler -> try handler model with e -> this.OnError e
            | Async handler -> 
                Async.StartWithContinuations(
                    computation = handler model, 
                    continuation = ignore, 
                    exceptionContinuation = this.OnError, 
                    cancellationContinuation = ignore))
#if DEBUG
        let observer = observer.Checked()
#endif
        let observer = Observer.Synchronize(observer, preventReentrancy = true)

        let scheduler = SynchronizationContextScheduler(SynchronizationContext.Current, alwaysPost = false)
        view.ObserveOn(scheduler)
            .Subscribe(observer)

    member this.Start model =
        use subcription = this.Activate model
        view.ShowDialog()

    member this.AsyncStart model =
        async {
            use subcription = this.Activate model
            return! view.Show()
        }

    abstract OnError : exn -> unit
    default this.OnError why = why |> PreserveStackTraceWrapper |> raise

    member this.Compose(childController : Controller<'EX, 'MX>, childView : PartialView<'EX, 'MX, _>, selector : 'Model -> 'MX ) = 
        let compositeView = view.Compose(childView, selector)
        { 
            new SupervisingController<_, _>(compositeView) with
                member __.InitModel model = 
                    this.InitModel model
                    childController.InitModel (selector model)
                member __.Dispatcher = function 
                    | Choice1Of2 e -> this.Dispatcher e
                    | Choice2Of2 e -> 
                        match childController.Dispatcher e with
                        | Sync handler -> Sync(selector >> handler)  
                        | Async handler -> Async(selector >> handler) 
        }

    member this.Compose(childController : Controller<_, _>, childView : PartialView<_, _, _>) = 
        this.Compose(childController, childView, id)

    static member (<+>) (parent : SupervisingController<_, _>,  (childController, childView, selector)) = 
        parent.Compose(childController, childView, selector)

    //Non-UI sources
    member this.Compose<'EX>(extension : 'EX -> EventHandler<_>, events : IObservable<'EX>) = 
        let compositeView = view.Compose(events)
        { 
            new SupervisingController<_, _>(compositeView) with
                member __.InitModel model = 
                    this.InitModel model
                member __.Dispatcher = function 
                    | Choice1Of2 e -> this.Dispatcher e
                    | Choice2Of2 e -> extension e 
        }

