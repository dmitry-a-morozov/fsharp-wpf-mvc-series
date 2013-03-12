namespace FSharp.Windows

open System.ComponentModel
open System

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events>, controller : IController<'Events, 'Model>) =

    member this.Start() =
        controller.InitModel model
        view.SetBindings model

        fun event -> 
            controller.EventHandler event model
        |> Observer.create
        |> Observer.preventReentrancy
        |> view.Subscribe 