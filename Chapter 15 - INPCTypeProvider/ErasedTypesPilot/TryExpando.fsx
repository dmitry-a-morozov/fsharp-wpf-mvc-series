
#r @"SampleModelPrototypes\bin\Debug\SampleModelPrototypes.dll"
#r @"ExpandoObject\bin\Debug\ExpandoObject.dll"

open System
open System.ComponentModel
open ExpandoObject.INPCTypeProvider

type ViewModels = NotifyPropertyChanged<"SampleModelPrototypes">

let model = ViewModels.Person(FirstName = "F#", LastName = "Amazing", DateOfBirth = DateTime.Parse("2005-01-01"))

let inpc : INotifyPropertyChanged = upcast model
inpc.PropertyChanged.Add(fun args -> printfn "Property %s. Model: %A" args.PropertyName model)

model.FirstName <- "Dmitry"
model.LastName <- "Morozov"
model.DateOfBirth <- DateTime.Parse("1974-01-01")
