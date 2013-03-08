namespace FSharp.Windows

open System.ComponentModel
open System.Reactive
open System
open System.Reflection

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
        view.Subscribe <| Observer.Synchronize(observer, preventReentrancy = true)

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

