namespace FSharp.Windows

open System.ComponentModel

type IController<'Events, 'Model when 'Model :> INotifyPropertyChanged> =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

module Controller = 

    let fromEventHandler callback = {
        new IController<'Events, 'Model> with
            member this.InitModel _ = ()
            member this.EventHandler = callback
    } 


