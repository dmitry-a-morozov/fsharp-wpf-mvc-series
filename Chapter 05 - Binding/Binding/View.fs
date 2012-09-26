namespace Mvc.Wpf

open System
open System.Windows

type IView<'Event, 'Model> =
    inherit IObservable<'Event>

    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Event, 'Model, 'W when 'W :> Window and 'W : (new : unit -> 'W)>(?window) = 

    let window = defaultArg window (new 'W())

    member this.Window = window
    static member (?) (view : View<'Event, 'Model, 'W>, name) = 
        match view.Window.FindName name with
        | null -> 
            match view.Window.TryFindResource name with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> unbox resource
        | control -> unbox control
    
    interface IView<'Event, 'Model> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model; 
            this.SetBindings model

    abstract EventStreams : IObservable<'Event> list
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type XamlView<'Event, 'Model>(resourceLocator) = 
    inherit View<'Event, 'Model, Window>(Application.LoadComponent resourceLocator |> unbox)
