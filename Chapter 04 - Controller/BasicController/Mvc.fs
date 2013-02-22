namespace FSharp.Windows

open System.ComponentModel

type Mvc<'Event, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Event>, eventHandler : 'Event -> 'Model -> unit) =

    member this.Start() =
        view.SetBindings model
        view.Subscribe (fun event -> eventHandler event model)
