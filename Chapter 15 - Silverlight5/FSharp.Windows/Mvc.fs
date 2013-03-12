namespace FSharp.Windows

open System.ComponentModel

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events>, controller : IController<'Events, 'Model>) =

    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> controller.EventHandler event model)