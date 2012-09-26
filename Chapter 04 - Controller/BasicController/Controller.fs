namespace Mvc.Wpf

open System.ComponentModel

[<AbstractClass>]
type Controller<'Event, 'Model when 'Model :> INotifyPropertyChanged>(view : IView<'Event>) =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Event -> 'Model -> unit)

    member this.Start model =
        this.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> this.EventHandler event model)


