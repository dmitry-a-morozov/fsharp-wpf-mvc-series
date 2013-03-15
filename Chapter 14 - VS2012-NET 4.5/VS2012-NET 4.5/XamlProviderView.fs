namespace FSharp.Windows.Sample

open System
open System.Windows
open FSharp.Windows
open FSharpx.TypeProviders.XamlProvider

[<AbstractClass>]
type XamlProviderView<'Events, 'Model>(window : Window) = 

    let mutable isOK = false

    interface IView<'Events, 'Model> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            window.DataContext <- model 
            this.SetBindings model
        member this.ShowDialog() = 
            window.ShowDialog() |> ignore
            isOK
        member this.Show() = 
            window.Show()
            window.Closed |> Event.map (fun _ -> isOK) |> Async.AwaitEvent 
        member this.Close isOK' = 
            isOK <- isOK'
            window.Close()

    abstract EventStreams : IObservable<'Events> list
    abstract SetBindings : 'Model -> unit



