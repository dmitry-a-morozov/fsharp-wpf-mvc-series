namespace FSharp.Windows

type IController<'Events, 'Model> =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

[<AbstractClass>]
type Controller<'Events, 'Model>() =

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

