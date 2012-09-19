namespace Mvc.Wpf

open System
open System.Windows

type IView<'E, 'M> =
    inherit IObservable<'E>

    abstract SetBindings : 'M -> unit

[<AbstractClass>]
type View<'E, 'M, 'W when 'W :> Window and 'W : (new : unit -> 'W)>(?window) = 

    let window = defaultArg window (new 'W())
    member this.Window = window
    
    interface IView<'E, 'M> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model; 
            this.SetBindings model

    abstract EventStreams : IObservable<'E> list
    abstract SetBindings : 'M -> unit

[<AbstractClass>]
type XamlView<'E, 'M>(resourceLocator) = 
    inherit View<'E, 'M, Window>(Application.LoadComponent resourceLocator |> unbox)

    static member (?) (view : View<_, _, _>, name) = 
        match view.Window.FindName name with
        | null -> invalidArg "Name" ("Cannot find control with name: " + name)
        | control -> unbox control 
