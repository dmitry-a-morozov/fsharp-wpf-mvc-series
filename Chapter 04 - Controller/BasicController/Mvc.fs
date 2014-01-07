namespace FSharp.Windows

open System.ComponentModel

module Mvc = 
    let start (model : #INotifyPropertyChanged) (view : IView<'Events>) (controller : IController<_, _>) = 
        controller.InitModel model
        view.SetBindings model
        view |> Observable.subscribe (fun event -> controller.EventHandler event model)

