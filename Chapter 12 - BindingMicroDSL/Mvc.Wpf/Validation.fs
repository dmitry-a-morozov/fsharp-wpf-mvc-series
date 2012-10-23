[<AutoOpen>]
module Mvc.Wpf.Validation

open System

let inline setError( SingleStepPropertySelector(propertyName, _) : PropertySelector< ^Model, _>) message model = 
    (^Model : (member SetError : string * string -> unit) (model, propertyName, message))

let inline clearError expr = setError expr null

let inline invalidIf( SingleStepPropertySelector(propertyName, getValue : ^Model -> _)) predicate message model = 
    if model |> getValue |> predicate 
    then 
        (^Model : (member SetError : string * string -> unit) (model, propertyName, message))

let inline assertThat expr predicate = invalidIf expr (not << predicate)

let inline objectRequired expr = invalidIf expr ((=) null) "Required field."
let inline valueRequired expr = assertThat expr (fun(x : Nullable<_>) -> x.HasValue) "Required field."
let inline textRequired expr = invalidIf expr String.IsNullOrWhiteSpace "Required field."

let inline positive expr = assertThat expr positive "Must be positive number."

