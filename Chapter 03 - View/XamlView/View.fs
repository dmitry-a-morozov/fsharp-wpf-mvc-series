namespace FSharp.Windows

open System
open System.Windows

type IView<'Events> =
    inherit IObservable<'Events>

    abstract SetBindings : obj -> unit

[<AbstractClass>]
type View<'Events, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>(?window) = 

    let window = defaultArg window (new 'Window())
    member this.Window = window

    interface IView<'Events> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model 
            this.SetBindings model

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : obj -> unit

[<AbstractClass>]
type XamlView<'Events>(resourceLocator) = 
    inherit View<'Events, Window>(resourceLocator |> Application.LoadComponent |> unbox)

    static member (?) (view : View<_, _>, name) = 
        match view.Window.FindName name with
        | null -> invalidArg "Name" ("Cannot find control with name: " + name)
        | control -> unbox control 
