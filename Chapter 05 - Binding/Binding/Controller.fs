namespace FSharp.Windows

open System.ComponentModel

type IController<'Events, 'Model when 'Model :> INotifyPropertyChanged> =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

[<AbstractClass>]
type Controller<'Events, 'Model when 'Model :> INotifyPropertyChanged>() =

    interface IController<'Events, 'Model> with
        member this.InitModel model = this.InitModel model
        member this.EventHandler = this.EventHandler

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

    static member FromEventHandler callback = {
        new IController<'Events, 'Model> with
            member this.InitModel _ = ()
            member this.EventHandler = callback
    } 

