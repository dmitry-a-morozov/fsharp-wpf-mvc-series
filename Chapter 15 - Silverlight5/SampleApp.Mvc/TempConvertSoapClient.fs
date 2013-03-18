[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SampleApp.Services

open System
open System.Net

type SampleApp.Services.TempConvertSoapClient with

    member this.AsyncCelsiusToFahrenheit(celsius : float) = 
        Async.FromContinuations <| fun(onSuccess, onError, onCancel) ->
            this.CelsiusToFahrenheitCompleted.Add <| fun eventArgs ->
                if eventArgs.Cancelled then onCancel(OperationCanceledException())
                elif eventArgs.Error <> null then onError eventArgs.Error
                else eventArgs.Result |> float |> onSuccess 
            celsius |> string |> this.CelsiusToFahrenheitAsync

    member this.AsyncFahrenheitToCelsius(fahrenheit : float) = 
        Async.FromContinuations <| fun(onSuccess, onError, onCancel) ->
            this.FahrenheitToCelsiusCompleted.Add <| fun eventArgs ->
                if eventArgs.Cancelled then onCancel(OperationCanceledException())
                elif eventArgs.Error <> null then onError eventArgs.Error
                else eventArgs.Result |> float |> onSuccess 
            fahrenheit |> string |> this.FahrenheitToCelsiusAsync


