namespace FSharp.Windows

open System.ComponentModel
open System
open System.Reflection

type Mvc =

    static member Start<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>, onException) =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe(fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> onException event exn
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = onException event,
                    cancellationContinuation = ignore
                )
        )

    static member Start(model, view, controller : IController<'Events, 'Model>) = 
        Mvc.Start(model, view, controller, onException = fun _ exn -> exn.Rethrow())

    static member inline Start(view : IView<'Events, 'Model>, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        Mvc.Start(model, view, controller)

