using System.Collections;

namespace MinimalApi.Tests.Swagger.SwaggerAiAssistant.UserPromptsTestData;

public class PetStoreUserPromptsTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "Could you remove pet in store with id 11?" };
        yield return new object[] { "Could you create pet in store with id 11 to name Boggi, and make his status available?" };
        yield return new object[] { "Update pet in store with id 10 to name Barsik, and make his status available?" };
        yield return new object[] { "Could you make an order for a pet with id 198773 with quantity 10?" };
        yield return new object[] { "Could you find order by id 10?" };
        yield return new object[] { "Could you create new user Alexander Whatson with email Alexander.Whatson@gmail.com with id 1000 ?" };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

