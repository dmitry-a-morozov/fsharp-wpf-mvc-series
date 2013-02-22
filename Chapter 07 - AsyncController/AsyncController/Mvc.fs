namespace FSharp.Windows

open System.ComponentModel

exception PreserveStackTraceWrapper of exn

type Mvc<'Event, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Event, 'Model>, controller : IController<'Event, 'Model>) =

    static let mutable defaultOnError : 'Event -> exn -> unit = fun event why -> why |> PreserveStackTraceWrapper |> raise
    static member DefaultOnError 
        with get() = defaultOnError
        and set errorHandler = defaultOnError <- errorHandler
    
    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe (fun event -> 
            match controller.Dispatcher event with
            | Sync handler -> 
                try 
                    handler model 
                with 
                    why -> this.OnError event why
            | Async handler -> 
                Async.StartWithContinuations(
                    computation = handler model, 
                    continuation = ignore, 
                    exceptionContinuation = this.OnError event, 
                    cancellationContinuation = ignore
                )
        )

    abstract OnError : ('Event -> exn -> unit)
    default this.OnError = defaultOnError

module Mvc = 

    let inline start(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        Mvc<'Event, ^Model>(model, view, controller).Start()

    