
#r @"SampleModelPrototypes\bin\Debug\SampleModelPrototypes.dll"
#r @"CustomRuntimeClass - GenTypes\bin\Debug\CustomRuntimeClass - GenTypes.dll"
#r @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\WindowsBase.dll"

open System
open System.ComponentModel
open System.Collections.ObjectModel
open System.Collections.Generic
open CustomRuntimeClass.INPCTypeProvider

type ViewModels = NotifyPropertyChanged<"SampleModelPrototypes">

let company = ViewModels.Company()

