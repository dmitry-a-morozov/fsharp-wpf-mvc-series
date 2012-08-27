namespace Mvc.Wpf

open System.ComponentModel

type EventHandler<'M> = 
    | Sync of ('M -> unit)
    | Async of ('M -> Async<unit>)

exception PreserveStackTraceWrapper of exn

[<AbstractClass>]
type Controller<'E, 'M when 'M :> INotifyPropertyChanged>(view : IView<'E, 'M>) =

    abstract InitModel : 'M -> unit
    abstract EventHandler : ('E -> EventHandler<'M>)

    member this.Start model =
        this.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun e -> 
            match this.EventHandler e with
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
type SyncController<'E, 'M when 'M :> INotifyPropertyChanged>(view) =
    inherit Controller<'E, 'M>(view)

    abstract EventHandler : ('E -> 'M -> unit)
    override this.EventHandler = fun e -> Sync(this.EventHandler e)
