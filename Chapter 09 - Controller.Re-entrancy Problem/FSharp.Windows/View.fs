namespace FSharp.Windows

open System
open System.Windows
open System.Windows.Controls

type IView<'Events, 'Model> =
    inherit IObservable<'Events>

    abstract SetBindings : 'Model -> unit

    abstract ShowDialog : unit -> bool
    abstract Show : unit -> Async<bool>
    abstract Close : bool -> unit

[<AbstractClass>]
type View<'Events, 'Model, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>(?window) = 

    let window = defaultArg window (new 'Window())
    let mutable isOK = false

    member this.Window = window
    static member (?) (view : View<'Events, 'Model, 'Window>, name) = 
        match view.Window.FindName name with
        | null -> 
            match view.Window.TryFindResource name with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> resource |> unbox
        | control -> control |> unbox
    
    interface IView<'Events, 'Model> with

        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model
            this.SetBindings model

        member this.ShowDialog() = 
            this.Window.ShowDialog() |> ignore
            isOK
        member this.Show() = 
            this.Window.Show()
            this.Window.Closed |> Event.map (fun _ -> isOK) |> Async.AwaitEvent 
        member this.Close isOK' = 
            isOK <- isOK'
            this.Window.Close()

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type XamlView<'Events, 'Model>(resourceLocator) = 
    inherit View<'Events, 'Model, Window>(resourceLocator |> Application.LoadComponent |> unbox)

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module View = 

    type IView<'Events, 'Model> with

        member this.OK() = this.Close true
        member this.Cancel() = this.Close false

        member this.CancelButton with set(value : Button) = value.Click.Add(ignore >> this.Cancel)
        member this.DefaultOKButton  
            with set(value : Button) = 
                value.IsDefault <- true
                value.Click.Add(ignore >> this.OK)

[<RequireQualifiedAccess>]
module List =
    open System.Windows.Controls

    let ofButtonClicks xs = xs |> List.map(fun(b : Button, value) -> b.Click |> Observable.mapTo value)
    
    