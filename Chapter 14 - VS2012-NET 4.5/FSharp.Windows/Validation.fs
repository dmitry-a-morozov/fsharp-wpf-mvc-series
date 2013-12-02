namespace FSharp.Windows

open System.ComponentModel

[<RequireQualifiedAccess>]
module Validation = 

    open System
    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.Patterns

    let (|PropertySelector|) (expr : Expr<('T -> 'a)>) = 
        match expr with 
        | Lambda(arg, PropertyGet( Some (Var selectOn), property, [])) -> 
            assert(arg.Name = selectOn.Name)
            property
        | _ -> 
            invalidArg "Expecting property getter expression" (string expr)

    let inline setErrors( PropertySelector property : Expr<('Model -> _)>) messages (model : #INotifyDataErrorInfo)= 
        (^Model : (member SetErrors : string * string[] -> unit) (model, property.Name, messages))

    let inline setError property message = setErrors property [| message |]

    let inline setErrorf model property = 
        Printf.ksprintf (fun message -> setError property message model) 

    let inline clearErrors property = setErrors property Array.empty 

    let inline invalidIf (PropertySelector property as expr : Expr<_ -> 'a>) predicate message model = 
        if model |> property.GetValue |> unbox<'a> |> predicate then setError expr message model

    let inline assertThat expr predicate = invalidIf expr (not << predicate)

    let inline objectRequired expr = invalidIf expr ((=) null) "Required field."

    let inline textRequired expr = invalidIf expr String.IsNullOrWhiteSpace "Required field."

    let inline valueRequired expr = assertThat expr (fun(x : Nullable<_>) -> x.HasValue) "Required field."

    let inline positive expr = assertThat expr (fun x -> x > LanguagePrimitives.GenericZero) "Must be positive number."

[<AutoOpen>]
module NotifyDataErrorInfo = 

    type INotifyDataErrorInfo with
        member this.IsValid = not this.HasErrors

