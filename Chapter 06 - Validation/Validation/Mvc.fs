namespace FSharp.Windows

open System.ComponentModel

type Mvc<'Event, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Event, 'Model>, controller : IController<'Event, 'Model>) =

    member this.Start() =
        controller.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> controller.EventHandler event model)

[<RequireQualifiedAccess>]
module Mvc = 

    let inline start(view, controller) = 
        let model = (^Model : (static member Create : unit -> ^Model ) ())
        Mvc<'Event, ^Model>(model, view, controller).Start()
