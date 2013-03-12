namespace FSharp.Windows

open System
open System.Windows
open System.Windows.Controls

type IView<'Events> =
    inherit IObservable<'Events>

    abstract SetBindings : obj -> unit

[<AbstractClass>]
type View<'Events, 'Page when 'Page :> Page and 'Page : (new : unit -> 'Page)>(?window) = 

    let window = defaultArg window (new 'Page())

    member this.Page = window
    static member (?) (view : View<_, _>, name) = 
        let w : Page = view.Page
        match view.Page.FindName name with
        | null -> invalidArg "Name" ("Cannot find child control named: " + name)
        | control -> unbox control
    
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
    inherit View<'Events, Page>(resourceLocator |> Application.LoadComponent |> unbox)


