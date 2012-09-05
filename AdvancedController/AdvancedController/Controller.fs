namespace Mvc.Wpf

type EventHandler<'M> = 
    | Sync of ('M -> unit)
    | Async of ('M -> Async<unit>)

exception PreserveStackTraceWrapper of exn

[<AbstractClass>]
type Controller<'E, 'M when 'M :> Model and 'M : not struct>(view : IView<'E, 'M>) =

    abstract InitModel : 'M -> unit
    abstract Dispatcher : ('E -> EventHandler<'M>)

    member this.Activate model =
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

    member this.Start model =
        use subcription = this.Activate model
        view.ShowDialog()

    member this.Start() = 
        let model = Model.Create()
        if this.Start model then Some model else None

    member this.AsyncStart model =
        async {
            use subcription = this.Activate model
            return! view.Show()
        }

    member this.AsyncStart() = 
        async {
            let model = Model.Create()
            let! isOk = this.AsyncStart model
            return if isOk then Some model else None
        }

    abstract OnError : exn -> unit
    default this.OnError why = why |> PreserveStackTraceWrapper |> raise

[<AbstractClass>]
type SyncController<'E, 'M when 'M :> Model and 'M : not struct>(view) =
    inherit Controller<'E, 'M>(view)

    abstract Dispatcher : ('E -> 'M -> unit)
    override this.Dispatcher = fun e -> Sync(this.Dispatcher e)

