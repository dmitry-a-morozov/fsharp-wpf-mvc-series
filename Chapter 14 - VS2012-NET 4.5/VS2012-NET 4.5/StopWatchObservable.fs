namespace Mvc.Wpf.Sample

open System
open System.Diagnostics
open System.Reactive.Linq
open Mvc.Wpf

type StopWatchObservable(frequency, failureFrequencyInSeconds) =
    let watch = Stopwatch.StartNew()
    let paused = ref false
    let generareFailures = ref false

    member this.Pause() = 
        watch.Stop()
        paused := true
    member this.Start() = 
        watch.Start()
        paused := false
    member this.Restart() = 
        watch.Restart()
        paused := false

    member this.GenerareFailures with set value = generareFailures := value

    interface IObservable<TimeSpan> with
        member this.Subscribe observer = 
            let xs = Observable.query {
                for _ in Observable.Interval(period = frequency) do
                where (not !paused)
                select (if !generareFailures && watch.Elapsed.TotalSeconds % failureFrequencyInSeconds < 1.0
                    then failwithf "failing every %.1f secs" failureFrequencyInSeconds
                    else watch.Elapsed)
            }
            xs.Subscribe(observer)
