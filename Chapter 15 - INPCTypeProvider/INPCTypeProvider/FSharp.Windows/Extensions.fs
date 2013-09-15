namespace FSharp.Windows
 
open System

[<AutoOpen>]
module Extensions = 

    open Microsoft.FSharp.Quotations
    open Microsoft.FSharp.Quotations.Patterns

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

    open System.Reactive.Linq

    type QueryBuilder internal() =
        member this.For(source : IObservable<_>, selector : _ -> IObservable<_>) = source.SelectMany(selector)
        member this.Zero() = Observable.Empty()
        [<CustomOperation("where", MaintainsVariableSpace = true, AllowIntoPattern = true)>]
        member this.Where(source : IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = source.Where(predicate)
        member this.Yield value = Observable.Return value
        [<CustomOperation("select", AllowIntoPattern = true)>]
        member this.Select(source : IObservable<_>, [<ProjectionParameter>] selector : _ -> _) = source.Select(selector)

    let query = QueryBuilder()

[<RequireQualifiedAccess>]
module Observer =

    open System.Reactive

    let create onNext = Observer.Create(Action<_>(onNext))

    let preventReentrancy observer = Observer.Synchronize(observer, preventReentrancy = true)

    let notifyOnDispatcher(observer : IObserver<_>) = 
        let dispatcher = System.Windows.Application.Current.Dispatcher 
        let invokeOnDispatcher f = if dispatcher.CheckAccess() then f() else dispatcher.BeginInvoke(Action(f), Array.empty) |> ignore 
        { 
            new IObserver<_> with 
                member __.OnNext value = invokeOnDispatcher(fun() -> observer.OnNext value)
                member __.OnError error = invokeOnDispatcher(fun() -> observer.OnError error)
                member __.OnCompleted() = invokeOnDispatcher observer.OnCompleted
        }  