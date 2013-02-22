namespace FSharp.Windows

open System.ComponentModel

type EventHandler<'Model> = 
    | Sync of ('Model -> unit)
    | Async of ('Model -> Async<unit>)

type IController<'Event, 'Model when 'Model :> INotifyPropertyChanged> =

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

[<AbstractClass>]
type Controller<'Event, 'Model when 'Model :> INotifyPropertyChanged>() =

    interface IController<'Event, 'Model> with
        member this.InitModel model = this.InitModel model
        member this.Dispatcher = this.Dispatcher

    abstract InitModel : 'Model -> unit
    abstract Dispatcher : ('Event -> EventHandler<'Model>)

    static member Create callback = {
        new IController<'Event, 'Model> with
            member this.InitModel _ = ()
            member this.Dispatcher = callback
    } 

[<AbstractClass>]
type SyncController<'Event, 'Model when 'Model :> INotifyPropertyChanged>() =
    inherit Controller<'Event, 'Model>()

    abstract Dispatcher : ('Event -> 'Model -> unit)
    override this.Dispatcher = fun e -> Sync(this.Dispatcher e)

[<AbstractClass>]
type AsyncInitController<'Event, 'Model when 'Model :> INotifyPropertyChanged>() =
    inherit Controller<'Event, 'Model>()

    abstract InitModel : 'Model -> Async<unit>
    override this.InitModel model = model |> this.InitModel |> Async.StartImmediate

    static member inline Create(controller : ^Controller) = {
        new IController<'Event, 'Model> with
            member this.InitModel model = (^Controller : (member InitModel : 'Model -> Async<unit>) (controller, model)) |> Async.StartImmediate
            member this.Dispatcher = (^Controller : (member Dispatcher : ('Event -> EventHandler<'Model>)) controller)
    } 
