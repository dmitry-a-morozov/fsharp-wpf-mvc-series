namespace FSharp.Windows

open System.ComponentModel
open System
open System.Reflection

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>) =

    static let internalPreserveStackTrace = lazy typeof<Exception>.GetMethod("InternalPreserveStackTrace", BindingFlags.Instance ||| BindingFlags.NonPublic)
    let mutable onError = fun _ exn -> 
        internalPreserveStackTrace.Value.Invoke(exn, [||]) |> ignore
        raise exn |> ignore
   //on CLR 4.5 is replaced by ExceptionDispatchInfo.Capture(exn).Throw()
    
    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe (fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> this.OnError event exn
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = this.OnError event,
                    cancellationContinuation = ignore
                )
        )

    abstract OnError : ('Events -> exn -> unit) with get, set
    default this.OnError with get() = onError and set value = onError <- value


