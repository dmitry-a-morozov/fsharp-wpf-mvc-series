namespace FSharp.Windows

open System.ComponentModel
open System
open System.Reflection

type Mvc private() =

    static member Activate<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events, 'Model>, controller : IController<'Events, 'Model>, onException) =
        controller.InitModel model
        view.SetBindings model

        Observer.create <| fun event -> 
            match controller.Dispatcher event with
            | Sync eventHandler ->
                try eventHandler model 
                with exn -> onException event exn
            | Async eventHandler -> 
                Async.StartWithContinuations(
                    computation = eventHandler model, 
                    continuation = ignore, 
                    exceptionContinuation = onException event,
                    cancellationContinuation = ignore)

        |> Observer.notifyOnDispatcher 
        |> Observer.preventReentrancy 
        |> view.Subscribe 

    static member Activate(model, view, controller : IController<'Events, 'Model>) = 
        Mvc.Activate(model, view, controller, onException = fun _ exn -> exn.Rethrow())

    static member inline Activate(view : IView<'Events, 'Model>, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        Mvc.Activate(model, view, controller)

    static member Start(model, dialog : #IModalWindow<_>, controller) = 
        use subscription = Mvc.Activate(model, dialog, controller)
        dialog.Show()

    