namespace global

open System
open System.ComponentModel

type IView<'Events> =
    inherit IObservable<'Events>

    abstract SetBindings : obj -> unit

type IController<'Events, 'Model> =
    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

module Mvc = 
    let start (model : #INotifyPropertyChanged) (view : IView<'Events>) (controller : IController<_, _>) = 
        controller.InitModel model
        view.SetBindings model
        view.Subscribe(fun event -> controller.EventHandler event model)
