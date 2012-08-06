namespace Mvc.Wpf

open System

[<AbstractClass>]
type Controller<'E, 'M when 'M :> Model and 'M : not struct>(view : IView<'E, 'M>) =

    abstract InitModel : 'M -> unit
    abstract EventHandler : ('E -> 'M -> unit)

    member this.Start model =
        this.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> this.EventHandler event model)


