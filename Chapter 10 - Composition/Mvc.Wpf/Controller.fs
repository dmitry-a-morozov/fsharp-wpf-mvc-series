namespace Mvc.Wpf

open System.ComponentModel
open System.Reactive

type EventHandler<'M> = 
    | Sync of ('M -> unit)
    | Async of ('M -> Async<unit>)

exception PreserveStackTraceWrapper of exn

[<AbstractClass>]
type Controller<'Event, 'Model when 'Model :> INotifyPropertyChanged>(view : IView<'Event, 'Model>) =

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

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
        view.Subscribe observer

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

[<AbstractClass>]
type SyncController<'Event, 'Model when 'Model :> INotifyPropertyChanged>(view) =
    inherit Controller<'Event, 'Model>(view)

    abstract Dispatcher : ('Event -> 'Model -> unit)
    override this.Dispatcher = fun e -> Sync(this.Dispatcher e)

