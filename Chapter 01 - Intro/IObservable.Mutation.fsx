
open System

type Events = 
    | Add of int 
    | Subtract of int

type View = IObservable<Events>  

type Model = { mutable State : int }

type EventHandler = Events -> Model -> unit
type Controller = EventHandler -> Model -> IObservable<Events> -> IDisposable



let subject = new Event<Events>()
let raiseEvents() = [Add 2; Subtract 1; Add 5] |> List.iter subject.Trigger

let view = subject.Publish
let model : Model = { State = 6 }
let eventHandler event model = 
    match event with
    | Add x -> model.State <- model.State + x 
    | Subtract x -> model.State <- model.State - x 

let mvc : Controller = fun eventhandler model view -> view.Subscribe(fun event -> eventHandler event model; printfn "Model: %A" model)

let subscription = view |> mvc eventHandler model

raiseEvents()

subscription.Dispose()
