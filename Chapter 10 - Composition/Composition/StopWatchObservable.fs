namespace Mvc.Wpf.Sample

open System
open System.Reactive.Linq
open System.Diagnostics

type StopWatchObservable(period : TimeSpan) =
    let watch = Stopwatch.StartNew()
    let paused = ref false

    member this.Pause() = 
        watch.Stop()
        paused := true
    member this.Start() = 
        watch.Start()
        paused := false
    member this.Restart() = 
        watch.Restart()
        paused := false

    interface IObservable<TimeSpan> with
        member this.Subscribe observer = 
            Observable.Interval(period)
                .Where(fun _ -> not !paused)
                .Select(fun _ -> watch.Elapsed)
                .Subscribe(observer)

