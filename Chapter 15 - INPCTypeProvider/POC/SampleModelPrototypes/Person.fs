namespace SampleModelPrototypes

open System

type Person = 
    {
        mutable FirstName : string 
        mutable LastName : string
        mutable DateOfBirth : System.DateTime
    }

    [<ReflectedDefinition>]
    member this.Name = String.Format("{0} {1}", this.LastName, this.FirstName)



type Company = {
    mutable Employees : System.Collections.ObjectModel.ObservableCollection<Person> 
}