namespace FSharp.Windows

type IController<'Event, 'Model> =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Event -> 'Model -> unit)

[<AbstractClass>]
type Controller<'Event, 'Model>() =

    interface IController<'Event, 'Model> with
        member this.InitModel model = this.InitModel model
        member this.EventHandler = this.EventHandler

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Event -> 'Model -> unit)

    static member FromEventHandler callback = {
        new IController<'Event, 'Model> with
            member this.InitModel _ = ()
            member this.EventHandler = callback
    } 

