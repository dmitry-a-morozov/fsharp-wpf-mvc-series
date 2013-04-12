namespace FSharp.Windows

open System
open System.Windows
open System.Windows.Controls

type IPartialView<'Events, 'Model> = 
    inherit IObservable<'Events>

    abstract SetBindings : 'Model -> unit

type IView<'Events, 'Model> =
    inherit IPartialView<'Events, 'Model>

    abstract ShowDialog : unit -> bool
    abstract Show : unit -> Async<bool>

[<AbstractClass>]
type PartialView<'Events, 'Model, 'Control when 'Control :> FrameworkElement>(control : 'Control) =

    member this.Control = control
    static member (?) (view : PartialView<'Events, 'Model, 'Control>, name) = 
        match view.Control.FindName name with
        | null -> 
            match view.Control.TryFindResource name with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> resource |> unbox
        | control -> control |> unbox
    
    interface IPartialView<'Events, 'Model> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            control.DataContext <- model
            this.SetBindings model

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Events, 'Model, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>(?window) = 
    inherit PartialView<'Events, 'Model, 'Window>(control = defaultArg window (new 'Window()))

    let mutable isOK = false

    interface IView<'Events, 'Model> with
        member this.ShowDialog() = 
            this.Control.ShowDialog() |> ignore
            isOK
        member this.Show() = 
            this.Control.Show()
            this.Control.Closed |> Event.map (fun _ -> isOK) |> Async.AwaitEvent 

    member this.Close isOK' = 
        isOK <- isOK'
        this.Control.Close()

    member this.OK() = this.Close true
    member this.Cancel() = this.Close false

    member this.CancelButton with set(value : Button) = value.Click.Add(ignore >> this.Cancel)
    member this.DefaultOKButton 
        with set(value : Button) = 
            value.IsDefault <- true
            value.Click.Add(ignore >> this.OK)

[<AbstractClass>]
type XamlView<'Events, 'Model>(resourceLocator) = 
    inherit View<'Events, 'Model, Window>(resourceLocator |> Application.LoadComponent |> unbox)

[<RequireQualifiedAccess>]
module List =
    open System.Windows.Controls

    let ofButtonClicks xs = xs |> List.map(fun(b : Button, value) -> b.Click |> Observable.mapTo value)
    
    