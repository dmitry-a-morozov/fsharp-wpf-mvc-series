namespace FSharp.Windows

open System.ComponentModel

type IController<'Event, 'Model when 'Model :> INotifyPropertyChanged> =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Event -> 'Model -> unit)

module Controller = 

    let fromEventHandler callback = {
        new IController<'Event, 'Model> with
            member this.InitModel _ = ()
            member this.EventHandler = callback
    } 


