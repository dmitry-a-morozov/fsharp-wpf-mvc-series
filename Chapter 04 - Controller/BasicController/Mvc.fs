namespace FSharp.Windows

open System.ComponentModel

type Mvc<'Events, 'Model when 'Model :> INotifyPropertyChanged>(model : 'Model, view : IView<'Events>, eventHandler : 'Events -> 'Model -> unit) =

    member this.Start() =
        view.SetBindings model
        view.Subscribe (fun event -> eventHandler event model)
