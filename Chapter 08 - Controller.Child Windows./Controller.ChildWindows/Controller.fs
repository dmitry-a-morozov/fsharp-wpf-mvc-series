namespace Mvc.Wpf

type EventHandler<'M> = 
    | Sync of ('M -> unit)
    | Async of ('M -> Async<unit>)

exception PreserveStackTraceWrapper of exn

[<AbstractClass>]
type Controller<'Event, 'Model when 'Model :> Model and 'Model : not struct>(view : IView<'Event, 'Model>) =

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

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
                    cancellationContinuation = ignore))

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
type SyncController<'Event, 'Model when 'Model :> Model and 'Model : not struct>(view) =
    inherit Controller<'Event, 'Model>(view)

    abstract Dispatcher : ('Event -> 'Model -> unit)
    override this.Dispatcher = fun e -> Sync(this.Dispatcher e)
