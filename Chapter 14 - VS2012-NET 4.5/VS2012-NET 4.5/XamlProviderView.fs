module Mvc.Wpf.Sample

open System
open FSharpx.TypeProviders.XamlProvider

[<AbstractClass>]
type XamlProviderPartialView<'Event, 'Model>(xaml: #XamlFile) =

    member this.XamlFile = xaml
    
    interface IPartialView<'Event, 'Model> with
        member this.Subscribe observer = 
            let xs = this.EventStreams |> List.reduce Observable.merge 
            xs.Subscribe observer
        member this.SetBindings model = 
            xaml.Root.DataContext <- model; 
            this.SetBindings model

    abstract EventStreams : IObservable<'Event> list
    abstract SetBindings : 'Model -> unit

[<AbstractClass>]
type View<'Event, 'Model>(xaml) = 
    inherit XamlProviderPartialView<'Event, 'Model>(xaml)

    let mutable isOK = false

    interface IView<'Event, 'Model> with
        member this.ShowDialog() = 
            this.XamlFile.Root.ShowDialog() |> ignore
            isOK
        member this.Show() = 
            this.Control.Show()
            this.Control.Closed |> Event.map (fun _ -> isOK) |> Async.AwaitEvent 
        member this.Close isOK' = 
            isOK <- isOK'
            this.Control.Close()




