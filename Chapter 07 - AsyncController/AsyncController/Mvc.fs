namespace FSharp.Windows

open System.ComponentModel
open System
open System.Reflection

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>) =

    static let defaultReraise =  
        let internalPreserveStackTrace = lazy typeof<Exception>.GetMethod("InternalPreserveStackTrace", BindingFlags.Instance ||| BindingFlags.NonPublic)
        fun exn ->
            internalPreserveStackTrace.Value.Invoke(exn, [||]) |> ignore
            raise exn |> ignore
    
    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe (fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> this.OnException(event, exn)
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = (fun exn -> this.OnException(event, exn)),
                    cancellationContinuation = ignore
                )
        )

    abstract OnException : 'Events * exn -> unit
    default this.OnException(_, exn) = defaultReraise exn 
    //defaultReraise on CLR 4.5 is replaced by ExceptionDispatchInfo.Capture(exn).Throw()

module Mvc = 

    let inline start(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        Mvc<'Events, ^Model>(model, view, controller).Start()

