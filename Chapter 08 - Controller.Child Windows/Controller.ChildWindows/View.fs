namespace Mvc.Wpf

open System
open System.Windows
open System.Windows.Controls

type IView<'Event, 'Model> =
    inherit IObservable<'Event>

    abstract SetBindings : 'Model -> unit

    abstract ShowDialog : unit -> bool
    abstract Show : unit -> Async<bool>
    abstract Close : bool -> unit

[<AbstractClass>]
type View<'Event, 'Model, 'Window when 'Window :> Window and 'Window : (new : unit -> 'Window)>(?window) = 

    let window = defaultArg window (new 'Window())
    let mutable isOK = false

    member this.Window = window
    static member (?) (view : View<'Event, 'Model, 'Window>, name) = 
        match view.Window.FindName name with
        | null -> 
            match view.Window.TryFindResource name with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> resource |> unbox
        | control -> control |> unbox
    
    interface IView<'Event, 'Model> with

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

    abstract EventStreams : IObservable<'Event> list
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type XamlView<'Event, 'Model>(resourceLocator) = 
    inherit View<'Event, 'Model, Window>(resourceLocator |> Application.LoadComponent |> unbox)

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module View = 

    type IView<'Event, 'Model> with

        member this.OK() = this.Close true
        member this.Cancel() = this.Close false

        member this.CancelButton with set(value : Button) = value.Click.Add(ignore >> this.Cancel)
        member this.DefaultOKButton 
            with set(value : Button) = 
                value.IsDefault <- true
                value.Click.Add(ignore >> this.OK)
