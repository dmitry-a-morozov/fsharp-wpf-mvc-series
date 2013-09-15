namespace FSharp.Windows
 
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

[<AutoOpen>]
module Extensions = 

    let inline undefined<'T> = raise<'T> <| NotImplementedException()

    exception PreserveStackTraceWrapper of exn

    type Exception with
        member this.Unwrap() = 
            match this with 
            | PreserveStackTraceWrapper inner -> inner.Unwrap()
            | exn -> exn

        member this.Rethrow() = 
            raise <| 
                match this with 
                | PreserveStackTraceWrapper _ as wrapped -> wrapped 
                | inner -> PreserveStackTraceWrapper inner

    type PropertySelector<'T, 'a> = Expr<('T -> 'a)>

    let (|SingleStepPropertySelector|) (expr : PropertySelector<'T, 'a>) = 
        match expr with 
        | Lambda(arg, PropertyGet( Some (Var selectOn), property, [])) -> 
            assert(arg.Name = selectOn.Name)
            property.Name, fun(this : 'T) -> property.GetValue(this, [||]) |> unbox<'a>
        | _ -> invalidArg "Property selector quotation" (string expr)
