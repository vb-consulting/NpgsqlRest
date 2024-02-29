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
          created_at TIMESTAMP NOT NULL default '2024-02-29'
        );

        comment on table customers is 'disabled';

        insert into customers
        (customer_id, name, email)
        values
        (1, 'test', 'email@email.com');


        create function get_latest_customer() returns customers language sql 
        as $$
        select * 
        from customers
        order by created_at
        limit 1
        $$;

        create function get_latest_customer_record() returns record language sql 
        as $$
        select * 
        from customers
        order by created_at
        limit 1
        $$;

        """);
    }
}

[Collection("TestFixture")]
public class SingleRecordTests(TestFixture test)
{
    [Fact]
    public async Task Test_get_latest_customer()
    {
        using var response = await test.Client.GetAsync($"/api/get-latest-customer/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("{\"customerId\":1,\"name\":\"test\",\"email\":\"email@email.com\",\"createdAt\":\"2024-02-29T00:00:00\"}");
    }

    [Fact]
    public async Task Test_get_latest_customer_record()
    {
        using var response = await test.Client.GetAsync($"/api/get-latest-customer-record/");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().Be("[\"1\",\"test\",\"email@email.com\",\"2024-02-29 00:00:00\"]");
    }
}