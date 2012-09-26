namespace Mvc.Wpf

open System.ComponentModel

[<AbstractClass>]
type Controller<'E, 'M when 'M :> INotifyPropertyChanged>(view : IView<'E, 'M>) =

    abstract InitModel : 'M -> unit
    abstract EventHandler : ('E -> 'M -> unit)

    member this.Start model =
        this.InitModel model
        view.SetBindings model
        view.Subscribe(callback = fun event -> this.EventHandler event model)


