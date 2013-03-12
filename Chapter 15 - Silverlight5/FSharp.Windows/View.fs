namespace FSharp.Windows

open System
open System.Windows
open System.Windows.Controls

type IView<'Events> =
    inherit IObservable<'Events>

    abstract SetBindings : obj -> unit

[<AbstractClass>]
type View<'Events, 'UserControl when 'UserControl :> UserControl and 'UserControl : (new : unit -> 'UserControl)>(?window) = 

    let control = defaultArg window (new 'UserControl())

    member this.Control = control
    static member (?) (view : View<_, _>, name) = 
        match view.Control.FindName name with
        | null -> invalidArg "Name" ("Cannot find child control named: " + name)
        | control -> unbox control
    
    interface IView<'Events> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
            //this.EventStreams |> List.reduce Observable.merge |> Observable.subscribe observer.OnNext
        member this.SetBindings model = 
            control.DataContext <- model
            this.SetBindings model

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : obj -> unit

[<AbstractClass>]
type XamlView<'Events>(resourceLocator) = 
    inherit View<'Events, UserControl>(resourceLocator |> Application.LoadComponent |> unbox)


