//Numeric up-down control
type View = IEvent<Events>  
and Events = Incr | Decr

type Model = { State : int }

type Controller = Model -> Events -> Model

type Mvc = Controller -> Model -> View -> Model

