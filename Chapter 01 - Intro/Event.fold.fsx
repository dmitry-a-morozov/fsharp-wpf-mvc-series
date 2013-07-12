

type Model = { State : int }

type View = IEvent<Add of int>  

and Events = 
    | Add of int 
    | Subtract of int

type Controller = Events -> Model -> Model

type Mvc = Controller -> Model -> IEvent<Events> -> Model

