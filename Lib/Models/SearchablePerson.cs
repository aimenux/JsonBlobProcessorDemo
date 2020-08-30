using Lib.AzureSearchStorage;

namespace Lib.Models
{
    public class SearchablePerson : Person, IAzureSearchModel
    {
        public SearchablePerson(Person person)
        {
            Id = person.Id;
            Name = person.Name;
            Age = person.Age;
            Email = person.Email;
        }
    }
}