namespace FSharp.Windows

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IController<'Events, 'Model> =

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Events -> EventHandler<'Model>)

[<AbstractClass>]
type Controller<'Events, 'Model>() =

    interface IController<'Events, 'Model> with
        member this.InitModel model = this.InitModel model
        member this.Dispatcher = this.Dispatcher

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Events -> EventHandler<'Model>)

    static member Create callback = {
        new IController<'Events, 'Model> with
            member this.InitModel _ = ()
            member this.Dispatcher = callback
    } 

[<AbstractClass>]
type AsyncInitController<'Events, 'Model>() =
    inherit Controller<'Events, 'Model>()

    abstract InitModel : 'Model -> Async<unit>
    override this.InitModel model = model |> this.InitModel |> Async.StartImmediate

    static member inline Create(controller : ^Controller) = {
        new IController<'Events, 'Model> with
            member this.InitModel model = (^Controller : (member InitModel : 'Model -> Async<unit>) (controller, model)) |> Async.StartImmediate
            member this.Dispatcher = (^Controller : (member Dispatcher : ('Events -> EventHandler<'Model>)) controller)
    } 
