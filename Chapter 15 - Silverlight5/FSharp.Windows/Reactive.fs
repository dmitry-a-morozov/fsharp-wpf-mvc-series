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
            member __.OnErrorCore exn = exn.Rethrow()
            member __.OnCompletedCore() = ()
    }

    let preventReentrancy observer = new AsyncLockObserver<_>(observer, new AsyncLock())

    let notifyOnDispatcher(observer : IObserver<_>) = 
        let dispatcher = System.Windows.Deployment.Current.Dispatcher 
        let invokeOnDispatcher f = if dispatcher.CheckAccess() then f() else dispatcher.BeginInvoke f |> ignore 
        { 
            new IObserver<_> with 
                member __.OnNext value = invokeOnDispatcher(fun() -> observer.OnNext value)
                member __.OnError error = invokeOnDispatcher(fun() -> observer.OnError error)
                member __.OnCompleted() = invokeOnDispatcher observer.OnCompleted
        }    


