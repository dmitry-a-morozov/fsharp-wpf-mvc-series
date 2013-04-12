namespace FSharp.Windows

open System.ComponentModel

type Mvc<'Event, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Event, 'Model>, controller : IController<'Event, 'Model>) =

    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> controller.EventHandler event model)

