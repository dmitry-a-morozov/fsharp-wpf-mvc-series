namespace global

open System

type IView<'Events, 'Model> =
    inherit IObservable<'Events>

    abstract SetBindings : 'Model -> unit

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IController<'Events, 'Model> =
    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

module Mvc = 
    let start (model : 'Model) (view : IView<'Events, 'Model>) (controller : IController<'Events, 'Model>) = 
        controller.InitModel model
        view.SetBindings model
        view |> Observable.subscribe (fun event -> controller.EventHandler event model)
