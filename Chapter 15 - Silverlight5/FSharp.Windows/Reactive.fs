namespace FSharp.Windows

open System 

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Observable = 

    let mapTo value = Observable.map(fun _ -> value)
    let unify first second = Observable.merge (Observable.map Choice1Of2 first) (Observable.map Choice2Of2 second)

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Observer = 

    open System.Reactive
    open System.Reactive.Concurrency

    let create onNext = {
        new ObserverBase<_>() with 
            member __.OnNextCore x = onNext x
            member __.OnErrorCore exn = raise exn 
            member __.OnCompletedCore() = ()
    }

    let preventReentrancy observer = new AsyncLockObserver<_>(observer, new AsyncLock())

