namespace FSharp.Windows

open System
open System.Windows
open System.Windows.Controls

type IView<'Events, 'Model> =
    inherit IObservable<'Events>

    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Events, 'Model, 'UserControl when 'UserControl :> UserControl and 'UserControl : (new : unit -> 'UserControl)>(?control) = 

    let control = defaultArg control (new 'UserControl())

    member this.Control = control
    static member (?) (view : View<'Events, 'Model, 'UserControl>, name) = 
        match view.Control.FindName name with
        | null -> 
            match view.Control.Resources.[name] with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> unbox resource
        | control -> unbox control
    
    interface IView<'Events, 'Model> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            control.DataContext <- model
            this.SetBindings model

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type XamlView<'Events, 'Model>(resourceLocator) = 
    inherit View<'Events, 'Model, UserControl>(resourceLocator |> Application.LoadComponent |> unbox)
