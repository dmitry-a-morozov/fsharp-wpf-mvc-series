namespace SampleModelPrototypes

type Person = {
    mutable FirstName : string 
    mutable LastName : string
    mutable DateOfBirth : System.DateTime
}

type Company = {
    mutable Employees : System.Collections.ObjectModel.ObservableCollection<Person> 
}