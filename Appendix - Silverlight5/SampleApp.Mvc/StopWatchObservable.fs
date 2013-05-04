namespace SampleApp

open System
open System.Threading

type StopWatchObservable(frequency, failureFrequencyInSeconds) =
    let mutable timer : Timer = null;
    let mutable acc = TimeSpan.Zero
    let source = {
            new IObservable<TimeSpan> with
                member this.Subscribe observer = 
                    let callBack _ = 
                        acc <- acc + frequency
                        observer.OnNext acc
                    timer <- new Timer(callBack, null, dueTime = TimeSpan.Zero, period = frequency)
                    upcast timer
    }

    let paused = ref false
    let generateFailures = ref false

    member this.Pause() = 
        let success = timer.Change(0, 0)
        assert success 
        paused := true
    member this.Start() = 
        let success = timer.Change(dueTime = TimeSpan.Zero, period = frequency)
        assert success 
        paused := false
    member this.Restart() = 
        acc <- TimeSpan.Zero
        paused := false

    member this.GenerateFailures with set value = generateFailures := value

    interface IObservable<TimeSpan> with
        member this.Subscribe observer = 
            source
            |> Observable.filter(fun _ -> not !paused)
            |> Observable.map(fun _ -> 
                if !generateFailures && acc.TotalSeconds % failureFrequencyInSeconds < 1.0
                then failwithf "failing every %.1f secs" failureFrequencyInSeconds
                else acc)
            |> Observable.subscribe observer.OnNext

