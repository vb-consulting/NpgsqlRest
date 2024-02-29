#pragma warning disable CS8602 // Dereference of a possibly null reference.
namespace NpgsqlRestTests;

public static partial class Database
{
    public static void SingleRecordTests()
    {
        script.Append(
            """
        create table customers (
          customer_id bigint not null PRIMARY KEY, 
          name text NOT NULL, 
          email text NULL, 
          created_at TIMESTAMP NOT NULL default now()
        );

        comment on table customers is 'disabled';

        insert into customers
        (customer_id, name, email)
        values
        (1, 'test', 'email@email.com');


        create function get_latest_customer(/*_before timestamp*/) 
        returns customers 
        language sql
        as $$
        select * 
        from customers
        --where
        --    _before is null or created_at < _before
        order by created_at
        limit 1
        $$;

        """);
    }
}

[Collection("TestFixture")]
public class SingleRecordTests(TestFixture test)
{
    /*
    [Fact]
    public async Task Test_get_latest_customer()
    {
        //var query = new QueryBuilder
        //{
        //    { "before", "" },
        //};
        //using var result = await test.Client.GetAsync($"/api/get-latest-customer/{query}");

        using var result = await test.Client.GetAsync($"/api/get-latest-customer/");
        var response = await result.Content.ReadAsStringAsync();

        result?.StatusCode.Should().Be(HttpStatusCode.OK);
        result?.Content?.Headers?.ContentType?.MediaType.Should().Be("application/json");
    }
    */
}