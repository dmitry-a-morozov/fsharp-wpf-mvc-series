namespace FSharp.Windows

type IController<'Events, 'Model> =

    abstract InitModel : 'Model -> unit
    abstract EventHandler : ('Events -> 'Model -> unit)

module Controller = 

    let fromEventHandler callback = {
        new IController<_, _> with
            member this.InitModel _ = ()
            member this.EventHandler = callback
    } 


