//Numeric up-down control
type Events = Incr | Decr
type View = IEvent<Events>  
type Model = { State : int }
type Controller = Model -> Events -> Model
type Mvc = Controller -> Model -> View -> Model

