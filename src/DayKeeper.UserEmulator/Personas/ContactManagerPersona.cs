using DayKeeper.UserEmulator.Client;

namespace DayKeeper.UserEmulator.Personas;

public sealed class ContactManagerPersona : IPersona
{
    public string Name => "ContactManager";

    public async Task SeedAsync(PersonaContext ctx, CancellationToken ct)
    {
        var personCount = ctx.DataFactory.RandomInt(15, 25);
        for (var i = 0; i < personCount; i++)
        {
            await SeedPersonAsync(ctx, ct).ConfigureAwait(false);
        }
    }

    public async Task RunIterationAsync(PersonaContext ctx, CancellationToken ct)
    {
        var roll = ctx.DataFactory.RandomInt(0, 99);
        try
        {
            if (roll < 12)
            {
                await CreatePersonAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 32)
            {
                await CreateContactMethodAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 44)
            {
                await CreateAddressAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 54)
            {
                await CreateImportantDateAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 69)
            {
                await UpdatePersonAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 81)
            {
                await GetPersonsAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 88)
            {
                await DeleteContactMethodAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 93)
            {
                await UpdateContactMethodAsync(ctx, ct).ConfigureAwait(false);
            }
            else if (roll < 96)
            {
                await DeletePersonAsync(ctx, ct).ConfigureAwait(false);
            }
            else
            {
                await UpdateAddressAsync(ctx, ct).ConfigureAwait(false);
            }
        }
        catch (GraphQLException)
        {
            // error already recorded in metrics
        }
        catch (HttpRequestException)
        {
            // error already recorded in metrics
        }
    }

    private static async Task SeedPersonAsync(PersonaContext ctx, CancellationToken ct)
    {
        var personId = await CreatePersonAsync(ctx, ct).ConfigureAwait(false);
        if (personId == Guid.Empty)
        {
            return;
        }

        await SeedContactMethodsAsync(ctx, personId, ct).ConfigureAwait(false);
        await SeedAddressesAsync(ctx, personId, ct).ConfigureAwait(false);
        await SeedImportantDatesAsync(ctx, personId, ct).ConfigureAwait(false);
    }

    private static async Task SeedContactMethodsAsync(PersonaContext ctx, Guid personId, CancellationToken ct)
    {
        var count = ctx.DataFactory.RandomInt(2, 3);
        for (var i = 0; i < count; i++)
        {
            await CreateContactMethodForPersonAsync(ctx, personId, ct).ConfigureAwait(false);
        }
    }

    private static async Task SeedAddressesAsync(PersonaContext ctx, Guid personId, CancellationToken ct)
    {
        var count = ctx.DataFactory.RandomInt(1, 2);
        for (var i = 0; i < count; i++)
        {
            await CreateAddressForPersonAsync(ctx, personId, ct).ConfigureAwait(false);
        }
    }

    private static async Task SeedImportantDatesAsync(PersonaContext ctx, Guid personId, CancellationToken ct)
    {
        var count = ctx.DataFactory.RandomInt(0, 2);
        for (var i = 0; i < count; i++)
        {
            await CreateImportantDateForPersonAsync(ctx, personId, ct).ConfigureAwait(false);
        }
    }

    private static async Task<Guid> CreatePersonAsync(PersonaContext ctx, CancellationToken ct)
    {
        try
        {
            var spaceId = ctx.GetWorkingSpaceId();
            var (firstName, lastName, notes) = ctx.DataFactory.GeneratePerson();
            var result = await ctx.ApiClient.GraphQLAsync(
                "CreatePerson",
                GraphQLOperations.CreatePerson,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { spaceId, firstName, lastName, notes } },
                ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
            var id = result.GetProperty("createPerson").GetProperty("person").GetProperty("id").GetGuid();
            ctx.PersonIds.Add(id);
            return id;
        }
        catch (GraphQLException)
        {
            return Guid.Empty;
        }
        catch (HttpRequestException)
        {
            return Guid.Empty;
        }
    }

    private static async Task CreateContactMethodAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.PersonIds.IsEmpty)
        {
            return;
        }

        var personId = ctx.DataFactory.PickRandom([.. ctx.PersonIds]);
        await CreateContactMethodForPersonAsync(ctx, personId, ct).ConfigureAwait(false);
    }

    private static async Task CreateContactMethodForPersonAsync(PersonaContext ctx, Guid personId, CancellationToken ct)
    {
        var (type, value, label, isPrimary) = ctx.DataFactory.GenerateContactMethod();
        var gqlType = ToGraphQLContactType(type);
        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateContactMethod",
            GraphQLOperations.CreateContactMethod,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { personId, type = gqlType, value, label, isPrimary } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        var id = result.GetProperty("createContactMethod").GetProperty("contactMethod").GetProperty("id").GetGuid();
        ctx.ContactMethodIds.Add(id);
    }

    private static async Task CreateAddressAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.PersonIds.IsEmpty)
        {
            return;
        }

        var personId = ctx.DataFactory.PickRandom([.. ctx.PersonIds]);
        await CreateAddressForPersonAsync(ctx, personId, ct).ConfigureAwait(false);
    }

    private static async Task CreateAddressForPersonAsync(PersonaContext ctx, Guid personId, CancellationToken ct)
    {
        var (label, street1, street2, city, state, postalCode, country, isPrimary) = ctx.DataFactory.GenerateAddress();
        var result = await ctx.ApiClient.GraphQLAsync(
            "CreateAddress",
            GraphQLOperations.CreateAddress,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { personId, label, street1, street2, city, state, postalCode, country, isPrimary } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
        var id = result.GetProperty("createAddress").GetProperty("address").GetProperty("id").GetGuid();
        ctx.AddressIds.Add(id);
    }

    private static async Task CreateImportantDateAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.PersonIds.IsEmpty)
        {
            return;
        }

        var personId = ctx.DataFactory.PickRandom([.. ctx.PersonIds]);
        await CreateImportantDateForPersonAsync(ctx, personId, ct).ConfigureAwait(false);
    }

    private static async Task CreateImportantDateForPersonAsync(PersonaContext ctx, Guid personId, CancellationToken ct)
    {
        var (label, dateValue) = ctx.DataFactory.GenerateImportantDate();
        await ctx.ApiClient.GraphQLAsync(
            "CreateImportantDate",
            GraphQLOperations.CreateImportantDate,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { personId, label, dateValue } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task UpdatePersonAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.PersonIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.PersonIds]);
        var (firstName, lastName, notes) = ctx.DataFactory.GeneratePerson();
        await ctx.ApiClient.GraphQLAsync(
            "UpdatePerson",
            GraphQLOperations.UpdatePerson,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, firstName, lastName, notes } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task GetPersonsAsync(PersonaContext ctx, CancellationToken ct)
    {
        var spaceId = ctx.GetWorkingSpaceId();
        await ctx.ApiClient.GraphQLAsync(
            "GetPersons",
            GraphQLOperations.GetPersons,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["spaceId"] = spaceId },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task DeleteContactMethodAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.ContactMethodIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.ContactMethodIds]);
        await ctx.ApiClient.GraphQLAsync(
            "DeleteContactMethod",
            GraphQLOperations.DeleteContactMethod,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task UpdateContactMethodAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.ContactMethodIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.ContactMethodIds]);
        var (type, value, label, isPrimary) = ctx.DataFactory.GenerateContactMethod();
        var gqlType = ToGraphQLContactType(type);
        await ctx.ApiClient.GraphQLAsync(
            "UpdateContactMethod",
            GraphQLOperations.UpdateContactMethod,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, type = gqlType, value, label, isPrimary } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task DeletePersonAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.PersonIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.PersonIds]);
        await ctx.ApiClient.GraphQLAsync(
            "DeletePerson",
            GraphQLOperations.DeletePerson,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static async Task UpdateAddressAsync(PersonaContext ctx, CancellationToken ct)
    {
        if (ctx.AddressIds.IsEmpty)
        {
            return;
        }

        var id = ctx.DataFactory.PickRandom([.. ctx.AddressIds]);
        var (label, street1, street2, city, state, postalCode, country, isPrimary) = ctx.DataFactory.GenerateAddress();
        await ctx.ApiClient.GraphQLAsync(
            "UpdateAddress",
            GraphQLOperations.UpdateAddress,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["input"] = new { id, label, street1, street2, city, state, postalCode, country, isPrimary } },
            ctx.Metrics, ctx.PersonaName, ctx.ArchetypeName, ct).ConfigureAwait(false);
    }

    private static string ToGraphQLContactType(string type) => type switch
    {
        "Phone" => "PHONE",
        "Email" => "EMAIL",
        _ => "OTHER",
    };
}
