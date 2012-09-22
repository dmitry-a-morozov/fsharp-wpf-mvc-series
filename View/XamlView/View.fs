namespace Mvc.Wpf

open System
open System.Windows

type IView<'Event, 'Model> =
    inherit IObservable<'Event>

    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Event, 'Model, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>(?window) = 

    let window = defaultArg window (new 'Window())
    member this.Window = window

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
    inherit View<'Event, 'Model, Window>(resourceLocator |> Application.LoadComponent |> unbox)

    static member (?) (view : View<_, _, _>, name) = 
        match view.Window.FindName name with
        | null -> invalidArg "Name" ("Cannot find control with name: " + name)
        | control -> unbox control 
