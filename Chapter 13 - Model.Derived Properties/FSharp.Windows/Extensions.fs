namespace FSharp.Windows
 
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns

[<AutoOpen>]
module Extensions = 

    let inline undefined<'T> = raise<'T> <| NotImplementedException()

    type PropertySelector<'T, 'a> = Expr<('T -> 'a)>

    let (|SingleStepPropertySelector|) (expr : PropertySelector<'T, 'a>) = 
        match expr with 
        | Lambda(arg, PropertyGet( Some (Var selectOn), property, [])) -> 
            assert(arg.Name = selectOn.Name)
            property.Name, fun(this : 'T) -> property.GetValue(this, [||]) |> unbox<'a>
        | _ -> invalidArg "Property selector quotation" (string expr)


[<RequireQualifiedAccess>]
module Observable =
    let mapTo value = Observable.map(fun _ -> value)
    let unify first second = Observable.merge (Observable.map Choice1Of2 first) (Observable.map Choice2Of2 second)

[<RequireQualifiedAccess>]
module Observer =

    open System.Reactive
    open System.Threading
    open System.Reactive.Concurrency

    let create onNext = Observer.Create(Action<_>(onNext))

    let notifyOnCurrentSynchronizationContext observer = 
        Observer.NotifyOn(observer, SynchronizationContextScheduler(SynchronizationContext.Current, alwaysPost = false))

    let preventReentrancy observer = Observer.Synchronize(observer, preventReentrancy = true)

