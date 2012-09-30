namespace Mvc.Wpf

open System.ComponentModel

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

exception PreserveStackTraceWrapper of exn

[<AbstractClass>]
type Controller<'Event, 'Model when 'Model :> INotifyPropertyChanged>(view : IView<'Event, 'Model>) =

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

    member this.Start model =
        this.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun e -> 
            match this.Dispatcher e with
            | Sync handler -> try handler model with e -> this.OnError e
            | Async handler -> 
                Async.StartWithContinuations(
                    computation = handler model, 
                    continuation = ignore, 
                    exceptionContinuation = this.OnError, 
                    cancellationContinuation = ignore
                )
        )

    abstract OnError : exn -> unit
    default this.OnError why = why |> PreserveStackTraceWrapper |> raise

[<AbstractClass>]
type SyncController<'Event, 'Model when 'Model :> INotifyPropertyChanged>(view) =
    inherit Controller<'Event, 'Model>(view)

    abstract Dispatcher : ('Event -> 'Model -> unit)
    override this.Dispatcher = fun e -> Sync(this.Dispatcher e)
