namespace Mvc.Wpf

open System.ComponentModel

[<AbstractClass>]
type Controller<'Event, 'Model>(view : IView<'Event, 'Model>) =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Event -> 'Model -> unit)

    member this.Start model =

        assert(match box model with | :? INotifyPropertyChanged -> true | _ -> false)

        this.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> this.EventHandler event model)


