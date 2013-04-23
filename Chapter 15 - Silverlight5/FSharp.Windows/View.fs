namespace FSharp.Windows

open System
open System.Windows
open System.Windows.Controls

type IView<'Events, 'Model> = 
    inherit IObservable<'Events>

    abstract SetBindings : 'Model -> unit

type IDialog<'T> =
    abstract Show : unit -> Async<'T>

[<AbstractClass>]
type View<'Events, 'Model, 'Control when 'Control :> FrameworkElement>(control : 'Control) =

    member this.Control = control
    static member (?) (view : View<'Events, 'Model, 'Control>, name) = 
        match view.Control.FindName name with
        | null -> 
            match view.Control.Resources.[name] with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> resource |> unbox
        | control -> control |> unbox
    
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
type Dialog<'Events, 'Model, 'ChildWindow when 'ChildWindow :> ChildWindow and 'ChildWindow : (new : unit -> 'ChildWindow)>(?childWindow) = 
    inherit View<'Events, 'Model, 'ChildWindow>(control = defaultArg childWindow (new 'ChildWindow()))

    interface IDialog<bool> with
        member this.Show() = 
            this.Control.Show()

            this.Control.Closed 
            |> Event.map (fun _ -> if this.Control.DialogResult.HasValue then this.Control.DialogResult.Value else false) 
            |> Async.AwaitEvent 

    member this.Close result = 
        this.Control.DialogResult <- Nullable result
        this.Control.Close()
    member this.OK() = this.Close true
    member this.Cancel() = this.Close false
    member this.CancelButton with set(value : Button) = value.Click.Add(ignore >> this.Cancel)
    member this.DefaultOKButton with set(value : Button) = value.Click.Add(ignore >> this.OK)

[<RequireQualifiedAccess>]
module List =
    open System.Windows.Controls

    let ofButtonClicks xs = xs |> List.map(fun(b : Button, value) -> b.Click |> Observable.mapTo value)