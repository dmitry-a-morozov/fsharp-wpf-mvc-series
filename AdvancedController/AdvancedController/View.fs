namespace Mvc.Wpf

open System
open System.Windows
open System.Windows.Controls

type IView<'E, 'M> =
    inherit IObservable<'E>

    abstract SetBindings : 'M -> unit

    abstract ShowDialog : unit -> bool
    abstract Show : unit -> Async<bool>
    abstract Close : bool -> unit

[<AbstractClass>]
type View<'E, 'M, 'W when 'W :> Window and 'W : (new : unit -> 'W)>(?window) = 

    let window = defaultArg window (new 'W())
    let mutable isOK = false

    member this.Window = window
    static member (?) (view : View<'E, 'M, 'W>, name) = 
        match view.Window.FindName name with
        | null -> 
            match view.Window.TryFindResource name with
            | null -> invalidArg "Name" ("Cannot find child control or resource named: " + name)
            | resource -> resource |> unbox
        | control -> control |> unbox
    
    interface IView<'E, 'M> with

        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model; 
            this.SetBindings model

        member this.ShowDialog() = 
            this.Window.ShowDialog() |> ignore
            isOK
        member this.Show() = 
            this.Window.Show()
            this.Window.Closed |> Event.map (fun _ -> isOK) |> Async.AwaitEvent 
        member this.Close OK = 
            isOK <- OK
            this.Window.Close()

    abstract EventStreams : IObservable<'E> list
    abstract SetBindings : 'M -> unit

[<AbstractClass>]
type XamlView<'E, 'M>(resourceLocator) = 
    inherit View<'E, 'M, Window>(Application.LoadComponent resourceLocator |> unbox)

[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module View = 

    type IView<'E, 'M> with

        member this.OK() = this.Close true
        member this.Cancel() = this.Close false

        member this.CancelButton with set(value : Button) = value.Click.Add(ignore >> this.Cancel)
        member this.OKButton 
            with set(value : Button) = 
                value.IsDefault <- true
                value.Click.Add(ignore >> this.OK)
