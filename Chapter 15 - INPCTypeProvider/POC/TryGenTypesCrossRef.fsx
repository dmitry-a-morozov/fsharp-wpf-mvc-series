
#r @"SampleModelPrototypes\bin\Debug\SampleModelPrototypes.dll"
#r @"CustomRuntimeClass - GenTypes\bin\Debug\CustomRuntimeClass - GenTypes.dll"
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\WindowsBase.dll"

open CustomRuntimeClass.INPCTypeProvider
open System
open System.Collections.ObjectModel
open System.ComponentModel

type ViewModels = NotifyPropertyChanged<"SampleModelPrototypes">

let model = ViewModels.Person(FirstName = "F#", LastName = "Amazing", DateOfBirth = DateTime.Parse("2005-01-01"))

let inpc : INotifyPropertyChanged = upcast model
inpc.PropertyChanged.Add(fun args -> printfn "Property %s. Model: %A" args.PropertyName model)

model.FirstName <- "Dmitry"
model.LastName <- "Morozov"
model.DateOfBirth <- DateTime.Parse("1974-01-01")

printfn "%s-%s-%A" model.FirstName model.LastName model.DateOfBirth

let company = ViewModels.Company()
let me = ViewModels.Person(FirstName = "Dmitry", LastName = "Morozov", DateOfBirth = DateTime.Parse("1974-08-23"))

company.Employees <- ObservableCollection [| me; ViewModels.Person(FirstName = "Ekaterina", LastName = "Blokhina", DateOfBirth = DateTime.Parse("1983-12-29")) |]

printfn "%A" company.Employees

