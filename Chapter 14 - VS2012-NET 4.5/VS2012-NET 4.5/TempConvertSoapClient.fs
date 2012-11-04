[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Mvc.Wpf.Sample

open Microsoft.FSharp.Data.TypeProviders

type TempConvert = WsdlService<"http://www.w3schools.com/webservices/tempconvert.asmx?WSDL">

type TempConvert.ServiceTypes.TempConvertSoapClient with

    member this.AsyncCelsiusToFahrenheit(celsius : float) = 
        celsius |> string |> this.CelsiusToFahrenheitAsync |> Async.AwaitTask 

    member this.AsyncFahrenheitToCelsius(fahrenheit : float) = 
        fahrenheit |> string |> this.FahrenheitToCelsiusAsync |> Async.AwaitTask 

