
open System

type Events = 
    | Add of int 
    | Subtract of int

type View = IObservable<Events>  

type Model = { mutable State : int }

type Controller = Events -> Model -> unit
type Mvc = Controller -> Model -> IObservable<Events> -> IDisposable



let subject = new Event<Events>()
let raiseEvents() = [Add 2; Subtract 1; Add 5] |> List.iter subject.Trigger

let view = subject.Publish
let model : Model = { State = 6 }
let controller event model = 
    match event with
    | Add x -> model.State <- model.State + x 
    | Subtract x -> model.State <- model.State - x 

let mvc : Mvc = fun controller model view -> view.Subscribe(fun event -> controller event model; printfn "Model: %A" model)

let subscription = view |> mvc controller model

raiseEvents()

subscription.Dispose()
